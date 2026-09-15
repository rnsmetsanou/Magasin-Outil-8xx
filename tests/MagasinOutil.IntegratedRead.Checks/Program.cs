using MagasinOutil.Core;
using MagasinOutil.Transport;

if (!OperatingSystem.IsWindows())
    throw new PlatformNotSupportedException("D2 integrated read checks require Windows named pipes.");
if (args.Length != 1)
    throw new ArgumentException("Usage: MagasinOutil.IntegratedRead.Checks [pipe-name]");

var password = Environment.GetEnvironmentVariable("WM_MAGASIN8XX_DEMO_PASSWORD");
if (string.IsNullOrWhiteSpace(password))
    throw new InvalidOperationException("WM_MAGASIN8XX_DEMO_PASSWORD is required for D2 checks.");

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS " + message);
}

var pipe = args[0];
using var client = new NamedPipeProductClient(pipe);
var clientId = "d2-check-" + Guid.NewGuid().ToString("N");
var login = await client.SignInAsync(new ProductSignInRequest(
    ProductSessionContract.Version,
    "consultation",
    password,
    clientId));
Check(login.IsAuthenticated && login.Session is not null,
    "Consultation establishes an authoritative session before magazine read.");
var session = login.Session!;

var direct = await client.ReadAsync(new ProductMagazineReadRequest(
    ProductMagazineReadContract.Version,
    session.SessionReference,
    clientId));
Check(direct.IsSuccess && direct.Snapshot is not null,
    "Session-governed product read reaches the CoreHost inventory authority.");
var snapshot = direct.Snapshot!;
Check(snapshot.Locations.Count == 140 && snapshot.Locations.Count(location => !location.Forbidden) == 137,
    "CoreHost snapshot exposes the 140 declared positions and 137 physical places.");
Check(snapshot.Locations.Single(location => location.Number == 27).Tool?.Id == 127,
    "Place 27 is mapped from the authoritative CoreHost snapshot to tool T127.");
Check(snapshot.Observation.Quality == "Good" && snapshot.Observation.Freshness == "Fresh",
    "Product snapshot preserves Machine quality Good and freshness Fresh.");
Check(snapshot.Observation.SessionGeneration > 0 && !string.IsNullOrWhiteSpace(snapshot.Observation.MachineSessionId),
    "Product snapshot exposes the authoritative Machine session generation and identifier.");
Check(snapshot.Observation.Origin == "Magasin8xx.Simulation" && snapshot.Observation.SourceTimestamp is null,
    "Product snapshot preserves origin and does not fabricate a source timestamp.");

var wrongClient = await client.ReadAsync(new ProductMagazineReadRequest(
    ProductMagazineReadContract.Version,
    session.SessionReference,
    clientId + "-other"));
Check(wrongClient.Status == ProductMagazineReadStatus.SessionInvalid && wrongClient.Snapshot is null,
    "Magazine read rejects replay of the opaque session by another client identity.");

var remote = new RemoteMagazineService(client, session.SessionReference, clientId);
await remote.RefreshAsync();
Check(remote.PlatformStatus.Connected && remote.Read().Count == 140,
    "HMI remote magazine adapter caches only the CoreHost snapshot and reports Core connected.");
Check(remote.PlatformStatus.Quality == "Good" && remote.PlatformStatus.Freshness == "Fresh" &&
      remote.PlatformStatus.MachineSessionGeneration > 0,
    "HMI platform status exposes quality, freshness and Machine session generation.");
var before = remote.Read().Single(location => location.Number == 27).Tool!;
var rejected = remote.Transfer(new SimulatedTransfer(27, before.Id, before.Revision,
    ToolPosition.Prepared, null, null));
Check(rejected.Outcome == EditOutcome.Rejected &&
      remote.Read().Single(location => location.Number == 27).Tool == before,
    "D2 HMI adapter performs no hidden local mutation before governed commands are integrated.");

var signOut = await client.SignOutAsync(new ProductSessionRequest(
    ProductSessionContract.Version,
    session.SessionReference,
    clientId));
Check(signOut.Status == ProductSessionStatus.Revoked,
    "D2 session is explicitly revoked before negative read verification.");
await remote.RefreshAsync();
Check(!remote.PlatformStatus.Connected,
    "HMI platform status becomes disconnected when its authoritative session is revoked.");
var afterLogout = await client.ReadAsync(new ProductMagazineReadRequest(
    ProductMagazineReadContract.Version,
    session.SessionReference,
    clientId));
Check(afterLogout.Status == ProductMagazineReadStatus.SessionInvalid && afterLogout.Snapshot is null,
    "Revoked session cannot read business inventory data through the product endpoint.");

Console.WriteLine("D2 Magasin 8xx CoreHost-backed HMI read integration: PASS");
