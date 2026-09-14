using System.Text.Json;
using Platform.Poc.Hmi.Runtime;
using Platform.Poc.Transport.Grpc;

if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Ce client local requiert Windows.");
var pipe = args.Length > 0 ? args[0] : "wm.magasin8xx.t01";
var clientId = args.Length > 1 ? args[1] : "diagnostic-" + Guid.NewGuid().ToString("N");
using var client = new NamedPipeInventoryClient(pipe);
var hmi = new ToolInventoryReadRuntime(client, "magasin-8xx-simulator", clientId);
var snapshot = await hmi.RefreshAsync();
Console.WriteLine(JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
