using MagasinOutil.Core;
using MagasinOutil.Transport;

if (!OperatingSystem.IsWindows())
    throw new PlatformNotSupportedException("D1 session checks require Windows named pipes.");
if (args.Length != 1)
    throw new ArgumentException("Usage: MagasinOutil.Session.Checks [pipe-name]");

var password = Environment.GetEnvironmentVariable("WM_MAGASIN8XX_DEMO_PASSWORD");
if (string.IsNullOrWhiteSpace(password))
    throw new InvalidOperationException("WM_MAGASIN8XX_DEMO_PASSWORD is required for D1 checks.");

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS " + message);
}

var pipe = args[0];
using var client = new NamedPipeProductClient(pipe);
var clientId = "d1-check-" + Guid.NewGuid().ToString("N");

var invalid = await client.SignInAsync(new ProductSignInRequest(
    ProductSessionContract.Version,
    "operateur",
    password + "-wrong",
    clientId));
Check(invalid.Status == ProductSignInStatus.InvalidCredentials && invalid.Session is null,
    "Wrong password is rejected without a session.");

async Task<ProductSessionView> SignInAsync(string userName)
{
    var result = await client.SignInAsync(new ProductSignInRequest(
        ProductSessionContract.Version,
        userName,
        password,
        clientId));
    Check(result.IsAuthenticated && result.Session is not null,
        $"Demo profile '{userName}' authenticates through the CoreHost authority.");
    return result.Session!;
}

async Task SignOutAsync(ProductSessionView session)
{
    var result = await client.SignOutAsync(new ProductSessionRequest(
        ProductSessionContract.Version,
        session.SessionReference,
        clientId));
    Check(result.Status == ProductSessionStatus.Revoked,
        $"Session for '{session.UserName}' is explicitly revoked on sign-out.");
}

var consultation = await SignInAsync("consultation");
Check(consultation.Permissions.Contains("magazine.read") && !consultation.Permissions.Contains("tool.prepare"),
    "Consultation profile remains read-only for tool preparation.");
await SignOutAsync(consultation);

var operateur = await SignInAsync("operateur");
Check(operateur.Permissions.Contains("tool.prepare") && operateur.Permissions.Contains("tool.load") &&
      !operateur.Permissions.Contains("identity.manage"),
    "Operator profile receives machine permissions without identity administration.");
var resolved = await client.ResolveAsync(new ProductSessionRequest(
    ProductSessionContract.Version,
    operateur.SessionReference,
    clientId));
Check(resolved.IsValid && resolved.Session?.SubjectId == operateur.SubjectId,
    "Opaque session resolves to the authoritative subject for its registered client.");
var wrongClient = await client.ResolveAsync(new ProductSessionRequest(
    ProductSessionContract.Version,
    operateur.SessionReference,
    clientId + "-other"));
Check(wrongClient.Status == ProductSessionStatus.WrongClient,
    "Opaque session cannot be replayed by another client identity.");
await SignOutAsync(operateur);
var revoked = await client.ResolveAsync(new ProductSessionRequest(
    ProductSessionContract.Version,
    operateur.SessionReference,
    clientId));
Check(revoked.Status == ProductSessionStatus.Revoked,
    "Signed-out session no longer resolves as valid.");

var regleur = await SignInAsync("regleur");
Check(regleur.Permissions.Contains("tool.data.edit") && regleur.Permissions.Contains("tool.correctors.edit"),
    "Tool setter profile adds tool-data and corrector permissions.");
await SignOutAsync(regleur);

var admin = await SignInAsync("admin");
Check(admin.Permissions.Contains("identity.manage") && admin.Permissions.Contains("license.install") &&
      !admin.Permissions.Contains("tool.prepare") && !admin.Permissions.Contains("tool.load"),
    "Administrator profile does not implicitly receive machine command permissions.");
Check(admin.AbsoluteExpiresAt > admin.IssuedAt && admin.PolicyRevision == 1,
    "Session exposes server-authoritative lifetime and policy revision.");
await SignOutAsync(admin);

Console.WriteLine("D1 Magasin 8xx real local identity and session integration: PASS");
