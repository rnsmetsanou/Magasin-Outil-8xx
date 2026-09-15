namespace MagasinOutil.Core;

public static class ProductMagazineReadContract
{
    public const int Version = 1;
    public const string ReadPermission = "magazine.read";
}

public enum ProductMagazineReadStatus
{
    Success = 0,
    SessionInvalid = 1,
    Forbidden = 2,
    Unavailable = 3,
    InvalidRequest = 4,
    Incompatible = 5,
}

public sealed record ProductMagazineReadRequest(
    int ContractVersion,
    string SessionReference,
    string ClientId);

public sealed record ProductMagazineObservation(
    string TargetId,
    string RuntimeEpoch,
    string MachineSessionId,
    long SessionGeneration,
    string Quality,
    string Freshness,
    DateTimeOffset ObservedAt,
    DateTimeOffset? SourceTimestamp,
    string Origin,
    string Consistency);

public sealed record ProductMagazineSnapshot(
    ProductMagazineObservation Observation,
    IReadOnlyList<Location> Locations);

public sealed record ProductMagazineReadResult(
    ProductMagazineReadStatus Status,
    ProductMagazineSnapshot? Snapshot,
    string Reason)
{
    public bool IsSuccess => Status == ProductMagazineReadStatus.Success && Snapshot is not null;
}

public interface IProductMagazineReadService
{
    ValueTask<ProductMagazineReadResult> ReadAsync(
        ProductMagazineReadRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ProductPlatformStatus(
    bool Connected,
    string? Quality,
    string? Freshness,
    long? MachineSessionGeneration,
    string? Origin,
    DateTimeOffset? ObservedAt,
    string Message);

public interface IIntegratedMagazineService : IMagazineService
{
    ProductPlatformStatus PlatformStatus { get; }
    ValueTask RefreshAsync(CancellationToken cancellationToken = default);
}
