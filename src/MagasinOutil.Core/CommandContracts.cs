namespace MagasinOutil.Core;

public static class ProductCommandContract
{
    public const int Version = 1;
}

public enum ProductCommandKind
{
    PrepareTool = 0,
    LoadTool = 1,
    EditTool = 2,
}

public enum ProductCommandStatus
{
    Completed = 0,
    SessionInvalid = 1,
    MissingPermission = 2,
    LicenseRejected = 3,
    BusinessRejected = 4,
    Conflict = 5,
    AdmissionUnavailable = 6,
    AdmissionConflict = 7,
    InvalidRequest = 8,
    Incompatible = 9,
    FailedAfterAdmission = 10,
    AlreadyAdmitted = 11,
}

public sealed record ProductCommandRequest(
    int ContractVersion,
    string SessionReference,
    string ClientId,
    string IntentId,
    ProductCommandKind Kind,
    int Location,
    int ToolId,
    long ExpectedRevision,
    int? ExpectedOccupantId = null,
    long? ExpectedOccupantRevision = null,
    string? Name = null,
    decimal? Wear = null,
    decimal? Length = null);

public sealed record ProductCommandResult(
    ProductCommandStatus Status,
    string IntentId,
    string? OperationId,
    string? SubjectId,
    string RequiredPermission,
    bool PermissionGranted,
    string RequiredEntitlement,
    string LicenseStatus,
    string? LicenseAuthorityVersion,
    string AdmissionStatus,
    string Message,
    DateTimeOffset RecordedAtUtc)
{
    public bool IsCompleted => Status == ProductCommandStatus.Completed;
}

public interface IProductMagazineCommandService
{
    ValueTask<ProductCommandResult> ExecuteAsync(
        ProductCommandRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ProductLicenseView(
    string Status,
    string InstallationId,
    string? LicenseId,
    long? RenewalVersion,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ExpiresAtUtc,
    IReadOnlyCollection<string> Capabilities,
    string Message);

public interface IProductLicenseReadService
{
    ValueTask<ProductLicenseView> ReadLicenseAsync(
        ProductSessionRequest request,
        CancellationToken cancellationToken = default);
}
