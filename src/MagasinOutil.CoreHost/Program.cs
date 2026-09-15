using MagasinOutil.CoreHost;
using MagasinOutil.Platform;
using MagasinOutil.Transport;
using Microsoft.Extensions.Hosting;
using Platform.Poc.Application.Contracts;
using Platform.Poc.Identity.Contracts;
using Platform.Poc.Identity.PasswordHashing.Argon2;
using Platform.Poc.Identity.Persistence.Sqlite;
using Platform.Poc.Identity.Runtime;
using Platform.Poc.Licensing.Contracts;
using Platform.Poc.Licensing.Cryptography;
using Platform.Poc.Licensing.Persistence.Sqlite;
using Platform.Poc.Licensing.Runtime;
using Platform.Poc.Persistence.Sqlite;

const string productId = "wm.magasin-outil-8xx";

if (!OperatingSystem.IsWindows())
    throw new PlatformNotSupportedException("Ce profil gRPC/tubes nommes requiert Windows.");
if (args.Length < 4 || args[0] != "--simulation")
    throw new ArgumentException("Usage: --simulation [nom-du-tube] [repertoire-etat] [tolerance-recul-horloge-secondes] [--demo-users]. Aucune connexion machine reelle disponible.");
if (!int.TryParse(args[3], out var maximumBackwardSkewSeconds) || maximumBackwardSkewSeconds < 0)
    throw new ArgumentException("La tolerance de recul d'horloge doit etre fournie explicitement en secondes et etre positive ou nulle.");

var pipe = args[1];
var stateDirectory = Path.GetFullPath(args[2]);
var demoUsersEnabled = args.Skip(4).Any(static value => string.Equals(value, "--demo-users", StringComparison.Ordinal));
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

// Qualification/demo composition only: no production issuer key is embedded in the pilot.
// A deployment must inject approved public verification keys before signed-license import is enabled.
var verification = new LicenseVerificationService(
    new ApprovedLicenseVerificationKeySet(Array.Empty<LicenseVerificationKey>()),
    [new EcdsaP256Sha256LicenseSignatureVerifier()]);
var licenseAuthority = new DurableLicenseAuthority(
    productId,
    installationIdentityService,
    licensingStore,
    verification,
    trustedTime);
var licenseState = await licenseAuthority.EvaluateAsync();

await using var host = NamedPipeProductHost.Create(pipe, application, sessions, sessions);
await host.StartAsync();
Console.WriteLine(
    $"READY {pipe} — simulation; admission-db={admissionStore.DatabasePath}; identity-db={identityStore.DatabasePath}; licensing-db={licensingStore.DatabasePath}; installation={installationIdentity.InstallationId}; license={licenseState.Status}; approved-license-keys=0; demo-users={(demoUsersEnabled ? "enabled" : "disabled")}; auth-throttle=durable; governed-magazine-read=enabled; clock-backward-skew-seconds={maximumBackwardSkewSeconds}.");
await host.WaitForShutdownAsync();
