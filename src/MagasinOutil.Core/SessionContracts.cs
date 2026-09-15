namespace MagasinOutil.Core;

public static class ProductSessionContract
{
    public const int Version = 1;
    public const string TargetId = "magasin-8xx-simulator";
}

public enum ProductSignInStatus
{
    Authenticated = 0,
    InvalidCredentials = 1,
    Unavailable = 2,
    InvalidRequest = 3,
    Incompatible = 4,
}

public enum ProductSessionStatus
{
    Valid = 0,
    NotFound = 1,
    Expired = 2,
    Revoked = 3,
    WrongClient = 4,
    Unavailable = 5,
    InvalidRequest = 6,
    Incompatible = 7,
}

public sealed record ProductSignInRequest(int ContractVersion, string UserName, string Password, string ClientId);

public sealed record ProductSessionView(
    string SessionReference,
    string SubjectId,
    string UserName,
    string DisplayName,
    IReadOnlyCollection<string> Permissions,
    DateTimeOffset IssuedAt,
    DateTimeOffset AbsoluteExpiresAt,
    long PolicyRevision);

public sealed record ProductSignInResult(ProductSignInStatus Status, ProductSessionView? Session, string Reason)
{
    public bool IsAuthenticated => Status == ProductSignInStatus.Authenticated && Session is not null;
}

public sealed record ProductSessionRequest(int ContractVersion, string SessionReference, string ClientId);
public sealed record ProductSessionResult(ProductSessionStatus Status, ProductSessionView? Session, string Reason)
{
    public bool IsValid => Status == ProductSessionStatus.Valid && Session is not null;
}

public interface IProductSessionService
{
    ValueTask<ProductSignInResult> SignInAsync(ProductSignInRequest request, CancellationToken cancellationToken = default);
    ValueTask<ProductSessionResult> ResolveAsync(ProductSessionRequest request, CancellationToken cancellationToken = default);
    ValueTask<ProductSessionResult> SignOutAsync(ProductSessionRequest request, CancellationToken cancellationToken = default);
}
