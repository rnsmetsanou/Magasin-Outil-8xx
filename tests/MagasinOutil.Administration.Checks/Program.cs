using MagasinOutil.Core;
using MagasinOutil.Transport;

if (!OperatingSystem.IsWindows())
    throw new PlatformNotSupportedException("D4 administration checks require Windows named pipes.");
if (args.Length != 1)
    throw new ArgumentException("Usage: MagasinOutil.Administration.Checks [pipe-name]");

var password = Environment.GetEnvironmentVariable("WM_MAGASIN8XX_DEMO_PASSWORD");
if (string.IsNullOrWhiteSpace(password))
    throw new InvalidOperationException("WM_MAGASIN8XX_DEMO_PASSWORD is required for D4 checks.");

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS " + message);
}

var pipe = args[0];
using var client = new NamedPipeProductClient(pipe);
var clientId = "d4-administration-check";

async Task<ProductSessionView> SignInAsync(string userName)
{
    var result = await client.SignInAsync(new ProductSignInRequest(
        ProductSessionContract.Version,
        userName,
        password,
        clientId));
    Check(result.IsAuthenticated && result.Session is not null,
        $"Profile '{userName}' authenticates before administration checks.");
    return result.Session!;
}

async Task SignOutAsync(ProductSessionView session)
{
    var result = await client.SignOutAsync(new ProductSessionRequest(
        ProductSessionContract.Version,
        session.SessionReference,
        clientId));
    Check(result.Status == ProductSessionStatus.Revoked,
        $"Session for '{session.UserName}' is revoked after administration checks.");
}

var operateur = await SignInAsync("operateur");
var operatorSnapshot = await client.ReadAdministrationAsync(new ProductSessionRequest(
    ProductSessionContract.Version,
    operateur.SessionReference,
    clientId));
Check(operatorSnapshot is not null, "Operator receives a session-governed administration snapshot.");
Check(operatorSnapshot!.License.Status == "Valid" && operatorSnapshot.License.Capabilities.Contains("tool-management"),
    "Operator administration view exposes the real valid product license.");
Check(!operatorSnapshot.CanViewUsers && operatorSnapshot.Users.Count == 0,
    "Operator cannot enumerate user profiles without identity/roles administration rights.");
Check(!operatorSnapshot.CanViewAudit && operatorSnapshot.AuditEntries.Count == 0,
    "Operator cannot read durable admission audit without audit.read.");
Check(operatorSnapshot.Session.Permissions.Contains("tool.prepare") && operatorSnapshot.Session.Permissions.Contains("tool.load"),
    "Operator view exposes its authoritative machine permissions.");
await SignOutAsync(operateur);

var admin = await SignInAsync("admin");
var adminSnapshot = await client.ReadAdministrationAsync(new ProductSessionRequest(
    ProductSessionContract.Version,
    admin.SessionReference,
    clientId));
Check(adminSnapshot is not null, "Administrator receives a session-governed administration snapshot.");
Check(adminSnapshot!.CanViewUsers && adminSnapshot.Users.Count == 4,
    "Administrator sees the four explicit demo profiles from the CoreHost catalogue.");
Check(adminSnapshot.CanViewAudit && adminSnapshot.AuditEntries.Count > 0,
    "Administrator sees durable admission audit entries produced by governed commands.");
var adminProfile = adminSnapshot.Users.Single(user => user.UserName == "admin");
Check(!adminProfile.Permissions.Contains("tool.prepare") && !adminProfile.Permissions.Contains("tool.load"),
    "Administrator profile visibly remains without implicit machine command permissions.");
var operatorProfile = adminSnapshot.Users.Single(user => user.UserName == "operateur");
Check(operatorProfile.Permissions.Contains("tool.prepare") && operatorProfile.Permissions.Contains("tool.load"),
    "Operator profile visibly carries the intended machine command permissions.");
Check(adminSnapshot.AuditEntries.Any(entry => entry.Action == "tool.prepare" && entry.Decision == "Admitted"),
    "Administration audit view exposes a real persisted tool.prepare admission.");
Check(adminSnapshot.AuditScope.Contains("admissions", StringComparison.OrdinalIgnoreCase),
    "Administration audit scope is explicitly limited to durable command admissions.");

var wrongClient = await client.ReadAdministrationAsync(new ProductSessionRequest(
    ProductSessionContract.Version,
    admin.SessionReference,
    clientId + "-other"));
Check(wrongClient is null, "Opaque session cannot read administration data for another client identity.");
await SignOutAsync(admin);

Console.WriteLine("D4 Magasin 8xx visual administration data integration: PASS");
