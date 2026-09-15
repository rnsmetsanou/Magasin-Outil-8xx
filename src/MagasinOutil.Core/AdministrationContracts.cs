namespace MagasinOutil.Core;

public static class ProductAdministrationContract
{
    public const int Version = 1;
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

public interface IProductAdministrationReadService
{
    ValueTask<ProductAdministrationSnapshot?> ReadAdministrationAsync(
        ProductSessionRequest request,
        CancellationToken cancellationToken = default);
}
