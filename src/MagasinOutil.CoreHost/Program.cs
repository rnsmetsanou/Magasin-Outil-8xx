using System.Text.Json;
using MagasinOutil.CoreHost;
using MagasinOutil.Platform;
using MagasinOutil.Transport;
using Microsoft.Extensions.Hosting;
using Platform.Poc.Application.Contracts;
using Platform.Poc.CrossCutting.Runtime.Identity;
using Platform.Poc.Foundation.Licensing;
using Platform.Poc.Identity.Contracts;
using Platform.Poc.Identity.PasswordHashing.Argon2;
using Platform.Poc.Identity.Persistence.Sqlite;
using Platform.Poc.Identity.Runtime;
using Platform.Poc.Licensing.Contracts;
using Platform.Poc.Licensing.Cryptography;
using Platform.Poc.Licensing.Persistence.Sqlite;
using Platform.Poc.Licensing.Runtime;
using Platform.Poc.Machine.Contracts.Operations.ToolHandling;
using Platform.Poc.Persistence.Sqlite;

const string productId = "wm.magasin-outil-8xx";

if (!OperatingSystem.IsWindows())
    throw new PlatformNotSupportedException("Ce profil gRPC/tubes nommes requiert Windows.");
if (args.Length < 4 || args[0] != "--simulation")
    throw new ArgumentException("Usage: --simulation [nom-du-tube] [repertoire-etat] [tolerance-recul-horloge-secondes] [--demo-users] [--demo-license]. Aucune connexion machine reelle disponible.");
if (!int.TryParse(args[3], out var maximumBackwardSkewSeconds) || maximumBackwardSkewSeconds < 0)
    throw new ArgumentException("La tolerance de recul d'horloge doit etre fournie explicitement en secondes et etre positive ou nulle.");

var pipe = args[1];
var stateDirectory = Path.GetFullPath(args[2]);
var optionalArgs = args.Skip(4).ToArray();
var demoUsersEnabled = optionalArgs.Any(static value => string.Equals(value, "--demo-users", StringComparison.Ordinal));
var demoLicenseEnabled = optionalArgs.Any(static value => string.Equals(value, "--demo-license", StringComparison.Ordinal));
if (demoLicenseEnabled && !demoUsersEnabled)
    throw new InvalidOperationException("--demo-license est reserve au profil explicite --demo-users.");
Directory.CreateDirectory(stateDirectory);

var admissionStore = new SqliteDurableAdmissionStore(Path.Combine(stateDirectory, "durable-authority.db"));
await admissionStore.InitializeAsync();

var identityDatabase = Path.Combine(stateDirectory, "identity.db");
var identityStore = new SqliteLocalAccountStore(identityDatabase);
await identityStore.InitializeAsync();
var identitySecurityStore = new SqliteIdentitySecurityStore(identityDatabase);
await identitySecurityStore.InitializeAsync();
var passwordHashing = new Argon2idPasswordHashingProvider();
var localAccounts = new LocalAccountService(identityStore, passwordHashing);
var authenticationDelayPolicy = new ProgressiveAuthenticationDelayPolicy(
    BeginAfterFailures: 5,
    DelaySchedule: [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30)]);
var authenticator = new RateLimitedLocalAuthenticator(
    localAccounts,
    identitySecurityStore,
    authenticationDelayPolicy);
var identityAuthority = new LocalIdentityAuthority(new SessionPolicySnapshot(
    Revision: 1,
    LocalInteractiveIdleTimeout: TimeSpan.FromMinutes(30),
    RemoteInteractiveIdleTimeout: TimeSpan.FromMinutes(10),
    AbsoluteLifetime: TimeSpan.FromHours(8),
    TemporaryCredentialLifetime: TimeSpan.FromMinutes(15),
    ProgressiveDelayAfterFailures: 5));

if (demoUsersEnabled)
{
    var demoPassword = Environment.GetEnvironmentVariable("WM_MAGASIN8XX_DEMO_PASSWORD");
    if (string.IsNullOrWhiteSpace(demoPassword))
        throw new InvalidOperationException("WM_MAGASIN8XX_DEMO_PASSWORD doit etre defini avec --demo-users.");
    await DemoIdentityBootstrap.EnsureDemoAccountsAsync(localAccounts, identityStore, demoPassword);
}

await using var application = await SimulatedInventoryApplication.StartAsync();
var sessions = new DemoSessionApplication(authenticator, identityStore, identityAuthority, application);

var licensingStore = new SqliteLicensingStateStore(Path.Combine(stateDirectory, "licensing.db"));
await licensingStore.InitializeAsync();
var installationIdentityService = new InstallationIdentityService(licensingStore);
var installationIdentity = await installationIdentityService.GetOrCreateAsync();
var trustedTime = new OfflineTrustedTimeAuthority(
    licensingStore,
    new TrustedTimePolicy(TimeSpan.FromSeconds(maximumBackwardSkewSeconds)));

var approvedKeys = new List<LicenseVerificationKey>();
if (demoLicenseEnabled)
{
    var publicKeyPath = Environment.GetEnvironmentVariable("WM_MAGASIN8XX_DEMO_LICENSE_PUBLIC_KEY");
    if (string.IsNullOrWhiteSpace(publicKeyPath) || !File.Exists(publicKeyPath))
        throw new InvalidOperationException("WM_MAGASIN8XX_DEMO_LICENSE_PUBLIC_KEY doit pointer vers une cle publique DEMO existante.");
    var keyJson = await File.ReadAllTextAsync(publicKeyPath);
    approvedKeys.Add(JsonSerializer.Deserialize<LicenseVerificationKey>(keyJson)
        ?? throw new InvalidDataException("Cle publique DEMO invalide."));
}

// No production issuer key is embedded. The explicit demo profile may inject one public DEMO key.
var verification = new LicenseVerificationService(
    new ApprovedLicenseVerificationKeySet(approvedKeys),
    [new EcdsaP256Sha256LicenseSignatureVerifier()]);
var licenseAuthority = new DurableLicenseAuthority(
    productId,
    installationIdentityService,
    licensingStore,
    verification,
    trustedTime);

if (demoLicenseEnabled)
{
    var licensePath = Environment.GetEnvironmentVariable("WM_MAGASIN8XX_DEMO_LICENSE_FILE");
    if (string.IsNullOrWhiteSpace(licensePath) || !File.Exists(licensePath))
        throw new InvalidOperationException("WM_MAGASIN8XX_DEMO_LICENSE_FILE doit pointer vers une licence signee DEMO existante.");
    var install = await licenseAuthority.InstallAsync(await File.ReadAllBytesAsync(licensePath));
    if (install.Status is not (DurableLicenseInstallStatus.Installed or DurableLicenseInstallStatus.AlreadyInstalled))
        throw new InvalidOperationException($"Installation de la licence DEMO refusee: {install.Status}/{install.VerificationStatus}.");
}

var licenseState = await licenseAuthority.EvaluateAsync();
var entitlementAuthority = new DurableLicenseEntitlementAuthority(
    licenseAuthority,
    [new KeyValuePair<EntitlementId, LicenseCapabilityId>(
        ToolHandlingEntitlements.ToolManagement,
        new LicenseCapabilityId("tool-management"))]);
var authorization = new PermissionAuthorizationService();
var commands = new DemoGovernedCommandApplication(
    sessions,
    application,
    authorization,
    entitlementAuthority,
    admissionStore,
    licenseAuthority,
    installationIdentityService);

await using var host = NamedPipeProductHost.Create(pipe, application, sessions, sessions, commands, commands);
await host.StartAsync();
Console.WriteLine(
    $"READY {pipe} — simulation; admission-db={admissionStore.DatabasePath}; identity-db={identityStore.DatabasePath}; licensing-db={licensingStore.DatabasePath}; installation={installationIdentity.InstallationId}; license={licenseState.Status}; approved-license-keys={approvedKeys.Count}; demo-users={(demoUsersEnabled ? "enabled" : "disabled")}; demo-license={(demoLicenseEnabled ? "enabled" : "disabled")}; auth-throttle=durable; governed-magazine-read=enabled; governed-commands=enabled; clock-backward-skew-seconds={maximumBackwardSkewSeconds}.");
await host.WaitForShutdownAsync();
