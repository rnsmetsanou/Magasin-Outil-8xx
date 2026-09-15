namespace MagasinOutil.Core;

public static class ProductAdministrationContract
{
    public const int Version = 1;
}

public enum ProductAdministrationReadStatus
{
    Success = 0,
    SessionInvalid = 1,
    Unavailable = 2,
    InvalidRequest = 3,
    Incompatible = 4,
}

public sealed record ProductUserProfileView(
    string UserName,
    string DisplayName,
    string Role,
    bool IsEnabled,
    IReadOnlyCollection<string> Permissions);

public sealed record ProductAuditEntryView(
    string EventId,
    string IntentId,
    string OperationId,
    DateTimeOffset RecordedAtUtc,
    string SubjectId,
    string ClientId,
    string TargetId,
    string Action,
    string Decision,
    long PolicyRevision);

public sealed record ProductAdministrationSnapshot(
    ProductSessionView Session,
    ProductLicenseView License,
    bool CanViewUsers,
    IReadOnlyCollection<ProductUserProfileView> Users,
    bool CanViewAudit,
    IReadOnlyCollection<ProductAuditEntryView> AuditEntries,
    string AuditScope,
    string Message);

public sealed record ProductAdministrationReadResult(
    ProductAdministrationReadStatus Status,
    ProductAdministrationSnapshot? Snapshot,
    string Reason)
{
    public bool IsSuccess => Status == ProductAdministrationReadStatus.Success && Snapshot is not null;
}

public interface IProductAdministrationReadService
{
    ValueTask<ProductAdministrationReadResult> ReadAdministrationAsync(
        ProductSessionRequest request,
        CancellationToken cancellationToken = default);
}
