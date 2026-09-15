using MagasinOutil.Platform;
using Microsoft.Extensions.Hosting;
using Platform.Poc.Licensing.Contracts;
using Platform.Poc.Licensing.Cryptography;
using Platform.Poc.Licensing.Persistence.Sqlite;
using Platform.Poc.Licensing.Runtime;
using Platform.Poc.Persistence.Sqlite;
using Platform.Poc.Transport.Grpc;

const string productId = "wm.magasin-outil-8xx";

if (!OperatingSystem.IsWindows())
    throw new PlatformNotSupportedException("Ce profil gRPC/tubes nommes requiert Windows.");
if (args.Length < 4 || args[0] != "--simulation")
    throw new ArgumentException("Usage: --simulation [nom-du-tube] [repertoire-etat] [tolerance-recul-horloge-secondes]. Aucune connexion machine reelle disponible.");
if (!int.TryParse(args[3], out var maximumBackwardSkewSeconds) || maximumBackwardSkewSeconds < 0)
    throw new ArgumentException("La tolerance de recul d'horloge doit etre fournie explicitement en secondes et etre positive ou nulle.");

var pipe = args[1];
var stateDirectory = Path.GetFullPath(args[2]);
Directory.CreateDirectory(stateDirectory);

var admissionStore = new SqliteDurableAdmissionStore(Path.Combine(stateDirectory, "durable-authority.db"));
await admissionStore.InitializeAsync();

var licensingStore = new SqliteLicensingStateStore(Path.Combine(stateDirectory, "licensing.db"));
await licensingStore.InitializeAsync();
var installationIdentityService = new InstallationIdentityService(licensingStore);
var installationIdentity = await installationIdentityService.GetOrCreateAsync();
var trustedTime = new OfflineTrustedTimeAuthority(
    licensingStore,
    new TrustedTimePolicy(TimeSpan.FromSeconds(maximumBackwardSkewSeconds)));

// Qualification composition only: no production issuer key is embedded in the pilot.
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

await using var application = await SimulatedInventoryApplication.StartAsync();
await using var host = NamedPipeInventoryHost.Create(pipe, application);
await host.StartAsync();
Console.WriteLine(
    $"READY {pipe} — simulation, lecture seule; admission-db={admissionStore.DatabasePath}; licensing-db={licensingStore.DatabasePath}; installation={installationIdentity.InstallationId}; license={licenseState.Status}; approved-license-keys=0; clock-backward-skew-seconds={maximumBackwardSkewSeconds}.");
await host.WaitForShutdownAsync();
