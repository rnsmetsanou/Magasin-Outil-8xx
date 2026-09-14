using MagasinOutil.Platform;
using Microsoft.Extensions.Hosting;
using Platform.Poc.Persistence.Sqlite;
using Platform.Poc.Transport.Grpc;

if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Ce profil gRPC/tubes nommés requiert Windows.");
if (args.Length < 1 || args[0] != "--simulation")
    throw new ArgumentException("Usage: --simulation [nom-du-tube] [repertoire-etat]. Aucune connexion machine réelle disponible.");

var pipe = args.Length > 1 ? args[1] : "wm.magasin8xx.t21";
var stateDirectory = args.Length > 2
    ? Path.GetFullPath(args[2])
    : Path.Combine(AppContext.BaseDirectory, "state");
Directory.CreateDirectory(stateDirectory);

var admissionStore = new SqliteDurableAdmissionStore(Path.Combine(stateDirectory, "durable-authority.db"));
await admissionStore.InitializeAsync();

await using var application = await SimulatedInventoryApplication.StartAsync();
await using var host = NamedPipeInventoryHost.Create(pipe, application);
await host.StartAsync();
Console.WriteLine($"READY {pipe} — simulation, lecture seule, stockage durable initialise: {admissionStore.DatabasePath}. Aucun compte produit ni licence validée.");
await host.WaitForShutdownAsync();
