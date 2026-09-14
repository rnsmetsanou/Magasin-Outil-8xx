using MagasinOutil.Platform;
using Microsoft.Extensions.Hosting;
using Platform.Poc.Transport.Grpc;

if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Ce profil gRPC/tubes nommés requiert Windows.");
if (args.Length < 1 || args[0] != "--simulation")
    throw new ArgumentException("Usage: --simulation [nom-du-tube]. Aucune connexion machine réelle disponible.");
var pipe = args.Length > 1 ? args[1] : "wm.magasin8xx.t01";
await using var application = await SimulatedInventoryApplication.StartAsync();
await using var host = NamedPipeInventoryHost.Create(pipe, application);
await host.StartAsync();
Console.WriteLine($"READY {pipe} — simulation, lecture seule, aucun compte produit ni licence validée.");
await host.WaitForShutdownAsync();
