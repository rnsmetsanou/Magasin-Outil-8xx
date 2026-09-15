using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MagasinOutil.Core;
using MagasinOutil.Platform;
using Platform.Poc.Application.Contracts;
using Platform.Poc.CrossCutting.Runtime.Identity;
using Platform.Poc.Foundation.Execution;
using Platform.Poc.Foundation.Identity;
using Platform.Poc.Foundation.Licensing;
using Platform.Poc.Licensing.Contracts;
using Platform.Poc.Licensing.Runtime;
using Platform.Poc.Machine.Contracts.Connectivity;
using Platform.Poc.Machine.Contracts.Operations;
using Platform.Poc.Machine.Contracts.Operations.ToolHandling;

namespace MagasinOutil.CoreHost;

internal sealed class DemoGovernedCommandApplication(
    DemoSessionApplication sessions,
    SimulatedInventoryApplication simulation,
    PermissionAuthorizationService authorization,
    IEntitlementAdmissionAuthority entitlementAuthority,
    IDurableAdmissionStore admissions,
    DurableLicenseAuthority licenseAuthority,
    InstallationIdentityService installationIdentity)
    : IProductMagazineCommandService, IProductLicenseReadService
{
    private readonly SemaphoreSlim _commandGate = new(1, 1);
    private static readonly TargetId Target = new(ProductSessionContract.TargetId);

    public async ValueTask<ProductCommandResult> ExecuteAsync(
        ProductCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        if (request.ContractVersion != ProductCommandContract.Version)
            return Result(ProductCommandStatus.Incompatible, request, null, null, "", false, "", "NotEvaluated", null, "NotAttempted", "Version de contrat incompatible.", now);
        if (!Guid.TryParse(request.IntentId, out var intentGuid) || intentGuid == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.SessionReference) || string.IsNullOrWhiteSpace(request.ClientId) ||
            request.Location <= 0 || request.ToolId <= 0 || request.ExpectedRevision < 0)
            return Result(ProductCommandStatus.InvalidRequest, request, null, null, "", false, "", "NotEvaluated", null, "NotAttempted", "Commande invalide.", now);

        var sessionResult = await sessions.ResolveAsync(
            new ProductSessionRequest(ProductSessionContract.Version, request.SessionReference, request.ClientId),
            cancellationToken).ConfigureAwait(false);
        if (!sessionResult.IsValid || sessionResult.Session is null)
            return Result(ProductCommandStatus.SessionInvalid, request, null, null, "", false, "", "NotEvaluated", null, "NotAttempted", "Session invalide ou expirée.", now);

        var session = sessionResult.Session;
        var subject = new SubjectContext(
            new SubjectId(session.SubjectId),
            SubjectKind.Human,
            true,
            session.Permissions.Select(static value => new PermissionId(value)));
        var requiredPermission = RequiredPermission(request.Kind);
        var authorizationDecision = authorization.Authorize(subject, requiredPermission);
        if (!authorizationDecision.IsAuthorized)
            return Result(ProductCommandStatus.MissingPermission, request, null, session.SubjectId,
                requiredPermission.Value, false, ToolHandlingEntitlements.ToolManagement.Value,
                "NotEvaluated", null, "NotAttempted", authorizationDecision.Reason, now);

        var licenseDecision = await entitlementAuthority.EvaluateAsync(
            ToolHandlingEntitlements.ToolManagement,
            cancellationToken).ConfigureAwait(false);
        if (!licenseDecision.IsGranted)
            return Result(ProductCommandStatus.LicenseRejected, request, null, session.SubjectId,
                requiredPermission.Value, true, ToolHandlingEntitlements.ToolManagement.Value,
                licenseDecision.Status.ToString(), licenseDecision.AuthorityVersion, "NotAttempted", licenseDecision.Reason, now);

        await _commandGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var intentId = new IntentId(intentGuid);
            var ownerSubject = new SubjectId(session.SubjectId);
            var ownerClient = new ClientId(request.ClientId.Trim());
            var canonical = Canonicalize(request);

            DurableAdmissionRecord? existing;
            try
            {
                existing = await admissions.FindAsync(intentId, ownerSubject, ownerClient, Target, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                return Result(ProductCommandStatus.AdmissionUnavailable, request, null, session.SubjectId,
                    requiredPermission.Value, true, ToolHandlingEntitlements.ToolManagement.Value,
                    licenseDecision.Status.ToString(), licenseDecision.AuthorityVersion, "LookupUnavailable",
                    "Admission durable indisponible : aucun effet n’est soumis.", DateTimeOffset.UtcNow);
            }

            if (existing is not null)
            {
                if (existing.CanonicalRequest != canonical)
                {
                    return Result(ProductCommandStatus.AdmissionConflict, request, null, session.SubjectId,
                        requiredPermission.Value, true, ToolHandlingEntitlements.ToolManagement.Value,
                        licenseDecision.Status.ToString(), licenseDecision.AuthorityVersion, "ContentConflict",
                        "Intent déjà utilisé avec un contenu différent.", DateTimeOffset.UtcNow);
                }

                return Result(ProductCommandStatus.AlreadyAdmitted, request, existing.OperationId.ToString(), session.SubjectId,
                    requiredPermission.Value, true, ToolHandlingEntitlements.ToolManagement.Value,
                    licenseDecision.Status.ToString(), licenseDecision.AuthorityVersion, "Existing",
                    "Intent déjà admis : l’effet n’est pas rejoué.", DateTimeOffset.UtcNow);
            }

            var preflight = Preflight(request);
            if (preflight is not null)
            {
                var status = preflight.Outcome == EditOutcome.Conflict
                    ? ProductCommandStatus.Conflict
                    : ProductCommandStatus.BusinessRejected;
                return Result(status, request, null, session.SubjectId,
                    requiredPermission.Value, true, ToolHandlingEntitlements.ToolManagement.Value,
                    licenseDecision.Status.ToString(), licenseDecision.AuthorityVersion, "NotAttempted", preflight.Message, DateTimeOffset.UtcNow);
            }

            var operationId = OperationId.New();
            var admittedAt = DateTimeOffset.UtcNow;
            var record = new DurableAdmissionRecord(
                intentId,
                operationId,
                ownerSubject,
                ownerClient,
                Target,
                canonical,
                new DurableAdmissionAuditEntry(
                    "audit-" + Guid.NewGuid().ToString("N"),
                    admittedAt,
                    ownerSubject,
                    ownerClient,
                    Target,
                    Action(request.Kind),
                    "Admitted",
                    session.PolicyRevision),
                admittedAt);

            DurableAdmissionCommitResult committed;
            try
            {
                committed = await admissions.TryCommitAsync(record, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return Result(ProductCommandStatus.AdmissionUnavailable, request, null, session.SubjectId,
                    requiredPermission.Value, true, ToolHandlingEntitlements.ToolManagement.Value,
                    licenseDecision.Status.ToString(), licenseDecision.AuthorityVersion, "Unavailable",
                    "Admission durable indisponible : aucun effet n’est soumis.", DateTimeOffset.UtcNow);
            }

            if (committed.Status == DurableAdmissionCommitStatus.Existing && committed.Record is not null)
            {
                return Result(ProductCommandStatus.AlreadyAdmitted, request, committed.Record.OperationId.ToString(), session.SubjectId,
                    requiredPermission.Value, true, ToolHandlingEntitlements.ToolManagement.Value,
                    licenseDecision.Status.ToString(), licenseDecision.AuthorityVersion, committed.Status.ToString(),
                    "Intent déjà admis : l’effet n’est pas rejoué.", DateTimeOffset.UtcNow);
            }

            if (committed.Status is DurableAdmissionCommitStatus.ContentConflict or DurableAdmissionCommitStatus.OwnershipConflict)
            {
                return Result(ProductCommandStatus.AdmissionConflict, request, null, session.SubjectId,
                    requiredPermission.Value, true, ToolHandlingEntitlements.ToolManagement.Value,
                    licenseDecision.Status.ToString(), licenseDecision.AuthorityVersion, committed.Status.ToString(),
                    "Intent déjà utilisé avec un autre contenu ou propriétaire.", DateTimeOffset.UtcNow);
            }

            if (committed.Status is not DurableAdmissionCommitStatus.Committed)
            {
                return Result(ProductCommandStatus.AdmissionUnavailable, request, null, session.SubjectId,
                    requiredPermission.Value, true, ToolHandlingEntitlements.ToolManagement.Value,
                    licenseDecision.Status.ToString(), licenseDecision.AuthorityVersion, committed.Status.ToString(),
                    "Admission durable indisponible ou incertaine : aucun effet n’est soumis.", DateTimeOffset.UtcNow);
            }

            var effect = Apply(request);
            if (effect.Outcome != EditOutcome.AppliedInSimulation)
            {
                return Result(ProductCommandStatus.FailedAfterAdmission, request, operationId.ToString(), session.SubjectId,
                    requiredPermission.Value, true, ToolHandlingEntitlements.ToolManagement.Value,
                    licenseDecision.Status.ToString(), licenseDecision.AuthorityVersion, committed.Status.ToString(),
                    "Commande admise mais effet simulé non confirmé : " + effect.Message, DateTimeOffset.UtcNow);
            }

            return Result(ProductCommandStatus.Completed, request, operationId.ToString(), session.SubjectId,
                requiredPermission.Value, true, ToolHandlingEntitlements.ToolManagement.Value,
                licenseDecision.Status.ToString(), licenseDecision.AuthorityVersion, committed.Status.ToString(),
                effect.Message, DateTimeOffset.UtcNow);
        }
        finally
        {
            _commandGate.Release();
        }
    }

    public async ValueTask<ProductLicenseView> ReadLicenseAsync(
        ProductSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await sessions.ResolveAsync(request, cancellationToken).ConfigureAwait(false);
        var installation = await installationIdentity.GetOrCreateAsync(cancellationToken).ConfigureAwait(false);
        if (!session.IsValid)
            return new ProductLicenseView("SessionInvalid", installation.InstallationId, null, null, null, null, [], "Session invalide ou expirée.");

        var evaluated = await licenseAuthority.EvaluateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return new ProductLicenseView(
            evaluated.Status.ToString(),
            installation.InstallationId,
            evaluated.License?.LicenseId.Value,
            evaluated.License?.RenewalVersion,
            evaluated.License?.ValidFromUtc,
            evaluated.License?.ExpiresAtUtc,
            evaluated.License?.Capabilities.Select(static value => value.Value).OrderBy(static value => value, StringComparer.Ordinal).ToArray() ?? [],
            evaluated.Status == InstalledLicenseEvaluationStatus.Valid ? "Licence signée valide." : "Licence non disponible pour l’admission de commande.");
    }

    private EditResult? Preflight(ProductCommandRequest request) => request.Kind switch
    {
        ProductCommandKind.PrepareTool => simulation.ValidateTransfer(ToTransfer(request, ToolPosition.Prepared)),
        ProductCommandKind.LoadTool => simulation.ValidateTransfer(ToTransfer(request, ToolPosition.Spindle)),
        ProductCommandKind.EditTool => simulation.ValidateEdit(ToEdit(request)),
        _ => new EditResult(EditOutcome.Rejected, "Commande non prise en charge."),
    };

    private EditResult Apply(ProductCommandRequest request) => request.Kind switch
    {
        ProductCommandKind.PrepareTool => simulation.Transfer(ToTransfer(request, ToolPosition.Prepared)),
        ProductCommandKind.LoadTool => simulation.Transfer(ToTransfer(request, ToolPosition.Spindle)),
        ProductCommandKind.EditTool => simulation.Apply(ToEdit(request)),
        _ => new EditResult(EditOutcome.Rejected, "Commande non prise en charge."),
    };

    private static SimulatedTransfer ToTransfer(ProductCommandRequest request, ToolPosition destination) =>
        new(request.Location, request.ToolId, request.ExpectedRevision, destination,
            request.ExpectedOccupantId, request.ExpectedOccupantRevision);

    private static EditTool ToEdit(ProductCommandRequest request)
    {
        if (request.Name is null || request.Wear is null)
            return new EditTool(request.Location, request.ToolId, request.ExpectedRevision, string.Empty, 0, request.Length);
        return new EditTool(request.Location, request.ToolId, request.ExpectedRevision, request.Name, request.Wear.Value, request.Length);
    }

    private static PermissionId RequiredPermission(ProductCommandKind kind) => kind switch
    {
        ProductCommandKind.PrepareTool => new PermissionId("tool.prepare"),
        ProductCommandKind.LoadTool => new PermissionId("tool.load"),
        ProductCommandKind.EditTool => new PermissionId("tool.data.edit"),
        _ => new PermissionId("unsupported.command"),
    };

    private static string Action(ProductCommandKind kind) => kind switch
    {
        ProductCommandKind.PrepareTool => "tool.prepare",
        ProductCommandKind.LoadTool => "tool.load",
        ProductCommandKind.EditTool => "tool.edit",
        _ => "unsupported.command",
    };

    private static CanonicalAdmissionRequest Canonicalize(ProductCommandRequest request)
    {
        var content = JsonSerializer.Serialize(new
        {
            kind = request.Kind.ToString(),
            location = request.Location,
            toolId = request.ToolId,
            expectedRevision = request.ExpectedRevision,
            expectedOccupantId = request.ExpectedOccupantId,
            expectedOccupantRevision = request.ExpectedOccupantRevision,
            name = request.Name,
            wear = request.Wear,
            length = request.Length,
        });
        var digest = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        return new CanonicalAdmissionRequest(
            DurableAdmissionContract.CanonicalRequestFormatVersion,
            Action(request.Kind),
            content,
            digest);
    }

    private static ProductCommandResult Result(
        ProductCommandStatus status,
        ProductCommandRequest request,
        string? operationId,
        string? subjectId,
        string requiredPermission,
        bool permissionGranted,
        string requiredEntitlement,
        string licenseStatus,
        string? licenseAuthorityVersion,
        string admissionStatus,
        string message,
        DateTimeOffset recordedAtUtc) =>
        new(status, request.IntentId, operationId, subjectId, requiredPermission, permissionGranted,
            requiredEntitlement, licenseStatus, licenseAuthorityVersion, admissionStatus, message, recordedAtUtc);
}
