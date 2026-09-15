using MagasinOutil.Core;
using MagasinOutil.Transport;

if (!OperatingSystem.IsWindows())
    throw new PlatformNotSupportedException("D3 governed command checks require Windows named pipes.");
if (args.Length != 2 || args[1] is not ("missing-license" or "licensed" or "replay-after-restart"))
    throw new ArgumentException("Usage: MagasinOutil.GovernedCommands.Checks [pipe-name] [missing-license|licensed|replay-after-restart]");

var password = Environment.GetEnvironmentVariable("WM_MAGASIN8XX_DEMO_PASSWORD");
if (string.IsNullOrWhiteSpace(password))
    throw new InvalidOperationException("WM_MAGASIN8XX_DEMO_PASSWORD is required for D3 checks.");

const string DurablePrepareIntent = "11111111-2222-4333-8444-555555555555";

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS " + message);
}

var pipe = args[0];
var mode = args[1];
using var client = new NamedPipeProductClient(pipe);
var clientId = "d3-check-stable-client";

async Task<ProductSessionView> SignInAsync(string userName)
{
    var result = await client.SignInAsync(new ProductSignInRequest(
        ProductSessionContract.Version,
        userName,
        password,
        clientId));
    Check(result.IsAuthenticated && result.Session is not null,
        $"Profile '{userName}' authenticates before governed command checks.");
    return result.Session!;
}

async Task<ProductMagazineSnapshot> ReadAsync(ProductSessionView session)
{
    var result = await client.ReadAsync(new ProductMagazineReadRequest(
        ProductMagazineReadContract.Version,
        session.SessionReference,
        clientId));
    Check(result.IsSuccess && result.Snapshot is not null,
        "Governed product snapshot is readable with the authoritative session.");
    return result.Snapshot!;
}

async Task SignOutAsync(ProductSessionView session)
{
    var result = await client.SignOutAsync(new ProductSessionRequest(
        ProductSessionContract.Version,
        session.SessionReference,
        clientId));
    Check(result.Status == ProductSessionStatus.Revoked,
        $"Session for '{session.UserName}' is revoked after D3 checks.");
}

static Location ToolLocation(ProductMagazineSnapshot snapshot, int toolId) =>
    snapshot.Locations.Single(location => location.Tool?.Id == toolId);

static Tool? Occupant(ProductMagazineSnapshot snapshot, ToolPosition position) =>
    snapshot.Locations.Select(static location => location.Tool).SingleOrDefault(tool => tool?.Position == position);

static ProductCommandRequest PrepareRequest(
    ProductSessionView session,
    string clientId,
    ProductMagazineSnapshot snapshot,
    string intentId)
{
    var location = ToolLocation(snapshot, 127);
    var tool = location.Tool!;
    var prepared = Occupant(snapshot, ToolPosition.Prepared);
    return new ProductCommandRequest(
        ProductCommandContract.Version,
        session.SessionReference,
        clientId,
        intentId,
        ProductCommandKind.PrepareTool,
        location.Number,
        tool.Id,
        tool.Revision,
        prepared?.Id,
        prepared?.Revision);
}

if (mode == "missing-license")
{
    var operateur = await SignInAsync("operateur");
    var license = await client.ReadLicenseAsync(new ProductSessionRequest(
        ProductSessionContract.Version,
        operateur.SessionReference,
        clientId));
    Check(license.Status == "Missing" && license.LicenseId is null,
        "CoreHost reports the real missing-license state before demo license installation.");

    var before = await ReadAsync(operateur);
    var beforeTool = ToolLocation(before, 127).Tool!;
    var request = PrepareRequest(operateur, clientId, before, Guid.NewGuid().ToString("D"));
    var result = await client.ExecuteAsync(request);
    Check(result.Status == ProductCommandStatus.LicenseRejected && result.PermissionGranted,
        "Operator permission alone cannot bypass a missing product license.");
    Check(result.RequiredPermission == "tool.prepare" && result.RequiredEntitlement == "tool-management" &&
          result.LicenseStatus == "MissingLicense" && result.AdmissionStatus == "NotAttempted",
        "Missing license is rejected before durable admission and exposes the governing decisions.");

    var after = await ReadAsync(operateur);
    var afterTool = ToolLocation(after, 127).Tool!;
    Check(afterTool.Position == beforeTool.Position && afterTool.Revision == beforeTool.Revision,
        "Missing-license rejection produces no simulated machine effect.");
    await SignOutAsync(operateur);
    Console.WriteLine("D3-A Magasin 8xx missing-license command gate: PASS");
    return;
}

if (mode == "replay-after-restart")
{
    var operateur = await SignInAsync("operateur");
    var snapshot = await ReadAsync(operateur);
    var before = ToolLocation(snapshot, 127).Tool!;
    Check(before.Position == ToolPosition.Magazine && before.Revision == 1,
        "Restarted simulation starts from its initial volatile state for the replay proof.");
    var replay = await client.ExecuteAsync(PrepareRequest(
        operateur,
        clientId,
        snapshot,
        DurablePrepareIntent));
    Check(replay.Status == ProductCommandStatus.AlreadyAdmitted && !string.IsNullOrWhiteSpace(replay.OperationId),
        "Durable Intent survives CoreHost restart and resolves as already admitted.");
    var after = await ReadAsync(operateur);
    var after127 = ToolLocation(after, 127).Tool!;
    Check(after127.Position == ToolPosition.Magazine && after127.Revision == 1,
        "Previously admitted Intent is not blindly resubmitted after CoreHost restart.");
    await SignOutAsync(operateur);
    Console.WriteLine("D3-C Magasin 8xx durable admission restart replay protection: PASS");
    return;
}

var consultation = await SignInAsync("consultation");
var consultationLicense = await client.ReadLicenseAsync(new ProductSessionRequest(
    ProductSessionContract.Version,
    consultation.SessionReference,
    clientId));
Check(consultationLicense.Status == "Valid" && consultationLicense.Capabilities.Contains("tool-management") &&
      consultationLicense.LicenseId is not null && consultationLicense.ExpiresAtUtc is { } licenseExpiry &&
      licenseExpiry > DateTimeOffset.UtcNow,
    "Signed installation-bound demo license is valid and exposes tool-management capability.");
var consultationSnapshot = await ReadAsync(consultation);
var consultationDenied = await client.ExecuteAsync(PrepareRequest(
    consultation,
    clientId,
    consultationSnapshot,
    Guid.NewGuid().ToString("D")));
Check(consultationDenied.Status == ProductCommandStatus.MissingPermission &&
      consultationDenied.RequiredPermission == "tool.prepare" && !consultationDenied.PermissionGranted &&
      consultationDenied.LicenseStatus == "NotEvaluated" && consultationDenied.AdmissionStatus == "NotAttempted",
    "Consultation is denied by human authorization before license/admission evaluation.");
var consultationAfter = await ReadAsync(consultation);
Check(ToolLocation(consultationAfter, 127).Tool!.Revision == ToolLocation(consultationSnapshot, 127).Tool!.Revision,
    "Permission rejection produces no simulated machine effect.");
await SignOutAsync(consultation);

var admin = await SignInAsync("admin");
var adminSnapshot = await ReadAsync(admin);
var adminDenied = await client.ExecuteAsync(PrepareRequest(
    admin,
    clientId,
    adminSnapshot,
    Guid.NewGuid().ToString("D")));
Check(adminDenied.Status == ProductCommandStatus.MissingPermission && adminDenied.RequiredPermission == "tool.prepare",
    "Administrator does not implicitly receive machine command permission.");
await SignOutAsync(admin);

var operateur = await SignInAsync("operateur");
var beforePrepare = await ReadAsync(operateur);
var prepareRequest = PrepareRequest(operateur, clientId, beforePrepare, DurablePrepareIntent);
var prepare = await client.ExecuteAsync(prepareRequest);
Check(prepare.Status == ProductCommandStatus.Completed && !string.IsNullOrWhiteSpace(prepare.OperationId),
    "Operator PrepareTool is completed through the governed CoreHost command path.");
Check(prepare.RequiredPermission == "tool.prepare" && prepare.PermissionGranted &&
      prepare.RequiredEntitlement == "tool-management" && prepare.LicenseStatus == "Granted" &&
      prepare.AdmissionStatus == "Committed",
    "PrepareTool exposes granted permission, granted signed license and durable admission.");

var afterPrepare = await ReadAsync(operateur);
var prepared127 = ToolLocation(afterPrepare, 127).Tool!;
var returned34 = ToolLocation(afterPrepare, 34).Tool!;
Check(prepared127.Position == ToolPosition.Prepared && returned34.Position == ToolPosition.Magazine,
    "Authoritative CoreHost snapshot confirms T127 prepared and previous T34 returned to magazine.");
var preparedRevision = prepared127.Revision;

var replay = await client.ExecuteAsync(prepareRequest);
Check(replay.Status == ProductCommandStatus.AlreadyAdmitted && replay.OperationId == prepare.OperationId,
    "Exact Intent replay returns the already admitted OperationId without resubmitting the effect.");
var afterReplay = await ReadAsync(operateur);
Check(ToolLocation(afterReplay, 127).Tool!.Revision == preparedRevision,
    "Exact Intent replay leaves the simulated machine state unchanged.");

var conflictingReplay = prepareRequest with { ToolId = 128 };
var conflict = await client.ExecuteAsync(conflictingReplay);
Check(conflict.Status == ProductCommandStatus.AdmissionConflict,
    "Reusing an admitted Intent with different content is rejected as an admission conflict.");

var spindleOccupant = Occupant(afterReplay, ToolPosition.Spindle);
var loadRequest = new ProductCommandRequest(
    ProductCommandContract.Version,
    operateur.SessionReference,
    clientId,
    Guid.NewGuid().ToString("D"),
    ProductCommandKind.LoadTool,
    27,
    127,
    preparedRevision,
    spindleOccupant?.Id,
    spindleOccupant?.Revision);
var load = await client.ExecuteAsync(loadRequest);
Check(load.Status == ProductCommandStatus.Completed && load.RequiredPermission == "tool.load" &&
      load.PermissionGranted && load.LicenseStatus == "Granted" && load.AdmissionStatus == "Committed",
    "Operator LoadTool is permission-, license- and admission-governed.");
var afterLoad = await ReadAsync(operateur);
Check(ToolLocation(afterLoad, 127).Tool!.Position == ToolPosition.Spindle &&
      ToolLocation(afterLoad, 12).Tool!.Position == ToolPosition.Magazine &&
      Occupant(afterLoad, ToolPosition.Prepared) is null,
    "Authoritative CoreHost snapshot confirms T127 in spindle, previous T12 returned and preparation cleared.");

var operator127 = ToolLocation(afterLoad, 127);
var deniedEdit = await client.ExecuteAsync(new ProductCommandRequest(
    ProductCommandContract.Version,
    operateur.SessionReference,
    clientId,
    Guid.NewGuid().ToString("D"),
    ProductCommandKind.EditTool,
    operator127.Number,
    operator127.Tool!.Id,
    operator127.Tool.Revision,
    Name: "Operator forbidden edit",
    Wear: -0.111m,
    Length: 145.000m));
Check(deniedEdit.Status == ProductCommandStatus.MissingPermission && deniedEdit.RequiredPermission == "tool.data.edit",
    "Operator cannot use the composite tool-data edit reserved for Tool Setter.");
await SignOutAsync(operateur);

var regleur = await SignInAsync("regleur");
var beforeEdit = await ReadAsync(regleur);
var edit127 = ToolLocation(beforeEdit, 127);
var edit = await client.ExecuteAsync(new ProductCommandRequest(
    ProductCommandContract.Version,
    regleur.SessionReference,
    clientId,
    Guid.NewGuid().ToString("D"),
    ProductCommandKind.EditTool,
    edit127.Number,
    edit127.Tool!.Id,
    edit127.Tool.Revision,
    Name: "Outil D3",
    Wear: -0.123m,
    Length: 145.000m));
Check(edit.Status == ProductCommandStatus.Completed && edit.RequiredPermission == "tool.data.edit" &&
      edit.PermissionGranted && edit.LicenseStatus == "Granted" && edit.AdmissionStatus == "Committed",
    "Tool Setter composite edit is permission-, license- and admission-governed.");
var afterEdit = await ReadAsync(regleur);
var edited127 = ToolLocation(afterEdit, 127).Tool!;
Check(edited127.Name == "Outil D3" && edited127.Wear == -0.123m && edited127.Length == 145.000m &&
      edited127.Revision == edit127.Tool.Revision + 1,
    "Authoritative snapshot confirms only the governed edit after durable admission.");

var wrongClient = await client.ExecuteAsync(new ProductCommandRequest(
    ProductCommandContract.Version,
    regleur.SessionReference,
    clientId + "-other",
    Guid.NewGuid().ToString("D"),
    ProductCommandKind.EditTool,
    edit127.Number,
    edited127.Id,
    edited127.Revision,
    Name: edited127.Name,
    Wear: edited127.Wear,
    Length: edited127.Length));
Check(wrongClient.Status == ProductCommandStatus.SessionInvalid,
    "Opaque session cannot authorize a governed command for another client identity.");
await SignOutAsync(regleur);

Console.WriteLine("D3-B Magasin 8xx governed signed-license commands: PASS");
