using MagasinOutil.Core;
using Platform.Poc.Application.Contracts;
using Platform.Poc.Foundation.Identity;
using Platform.Poc.Identity.Contracts;
using Platform.Poc.Identity.Runtime;
using Platform.Poc.Machine.Contracts.Connectivity;

namespace MagasinOutil.CoreHost;

internal sealed class DemoSessionApplication(
    RateLimitedLocalAuthenticator authenticator,
    ILocalAccountStore accountStore,
    LocalIdentityAuthority authority) : IProductSessionService
{
    private readonly object _registrationGate = new();
    private readonly HashSet<string> _registeredSubjects = new(StringComparer.Ordinal);
    private static readonly TargetId Target = new(ProductSessionContract.TargetId);

    public async ValueTask<ProductSignInResult> SignInAsync(
        ProductSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ContractVersion != ProductSessionContract.Version)
            return new(ProductSignInStatus.Incompatible, null, "Version de contrat incompatible.");
        if (!ValidText(request.UserName, 128) || string.IsNullOrEmpty(request.Password) || request.Password.Length > 512 ||
            !ValidText(request.ClientId, 128))
            return new(ProductSignInStatus.InvalidRequest, null, "Demande de connexion invalide.");

        var authentication = await authenticator.AuthenticateAsync(request.UserName, request.Password, cancellationToken)
            .ConfigureAwait(false);
        if (!authentication.IsAuthenticated || authentication.Identity is null)
        {
            return authentication.Status switch
            {
                ProtectedLocalAuthenticationStatus.Throttled =>
                    new(ProductSignInStatus.Throttled, null, "Trop de tentatives. Réessayer après le délai indiqué.", authentication.RetryAfter),
                ProtectedLocalAuthenticationStatus.Unavailable =>
                    new(ProductSignInStatus.Unavailable, null, "Autorité d'identité indisponible."),
                _ => new(ProductSignInStatus.InvalidCredentials, null, "Utilisateur ou mot de passe incorrect."),
            };
        }

        var identity = authentication.Identity;
        EnsureRegistered(identity);
        var clientId = new ClientId(request.ClientId.Trim());
        var reference = authority.IssueAfterAuthentication(
            identity.SubjectId,
            clientId,
            Target,
            ClientSessionAccessKind.LocalInteractive);
        var resolved = await authority.ResolveAsync(reference, clientId, Target, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsValid || resolved.Session is null)
            return new(ProductSignInStatus.Unavailable, null, "La session n'a pas pu être établie.");

        return new(ProductSignInStatus.Authenticated, ToView(resolved.Session, identity.UserName, identity.DisplayName), "Authentifié.");
    }

    public async ValueTask<ProductSessionResult> ResolveAsync(
        ProductSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateSessionRequest(request);
        if (validation is not null) return validation;

        var reference = new ClientSessionReference(request.SessionReference);
        var clientId = new ClientId(request.ClientId.Trim());
        var resolved = await authority.ResolveAsync(reference, clientId, Target, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsValid || resolved.Session is null)
            return new(Map(resolved.Status), null, resolved.Status.ToString());

        authority.RecordHumanActivity(reference, clientId, Target);
        var account = await accountStore.FindBySubjectAsync(resolved.Session.Subject.SubjectId, cancellationToken)
            .ConfigureAwait(false);
        if (account is null)
            return new(ProductSessionStatus.Unavailable, null, "Compte de session introuvable.");

        return new(ProductSessionStatus.Valid, ToView(resolved.Session, account.UserName, account.DisplayName), "Session valide.");
    }

    public async ValueTask<ProductSessionResult> SignOutAsync(
        ProductSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateSessionRequest(request);
        if (validation is not null) return validation;

        var reference = new ClientSessionReference(request.SessionReference);
        var clientId = new ClientId(request.ClientId.Trim());
        var resolved = await authority.ResolveAsync(reference, clientId, Target, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsValid)
            return new(Map(resolved.Status), null, resolved.Status.ToString());

        authority.Revoke(reference);
        return new(ProductSessionStatus.Revoked, null, "Session fermée.");
    }

    private ProductSessionResult? ValidateSessionRequest(ProductSessionRequest request)
    {
        if (request.ContractVersion != ProductSessionContract.Version)
            return new(ProductSessionStatus.Incompatible, null, "Version de contrat incompatible.");
        if (!ValidText(request.SessionReference, 256) || !ValidText(request.ClientId, 128))
            return new(ProductSessionStatus.InvalidRequest, null, "Référence de session invalide.");
        return null;
    }

    private void EnsureRegistered(AuthenticatedLocalIdentity identity)
    {
        lock (_registrationGate)
        {
            if (!_registeredSubjects.Add(identity.SubjectId.Value)) return;
            authority.RegisterHuman(identity.SubjectId, identity.UserName, identity.DisplayName, identity.Permissions);
        }
    }

    private static ProductSessionView ToView(ResolvedClientSession session, string userName, string displayName) =>
        new(
            session.Reference.Value,
            session.Subject.SubjectId.Value,
            userName,
            displayName,
            session.Subject.Permissions.Select(static permission => permission.Value).OrderBy(static value => value, StringComparer.Ordinal).ToArray(),
            session.IssuedAt,
            session.AbsoluteExpiresAt,
            session.PolicyRevision);

    private static ProductSessionStatus Map(SessionResolutionStatus status) => status switch
    {
        SessionResolutionStatus.Valid => ProductSessionStatus.Valid,
        SessionResolutionStatus.NotFound => ProductSessionStatus.NotFound,
        SessionResolutionStatus.Expired => ProductSessionStatus.Expired,
        SessionResolutionStatus.Revoked => ProductSessionStatus.Revoked,
        SessionResolutionStatus.WrongClient => ProductSessionStatus.WrongClient,
        SessionResolutionStatus.Unavailable => ProductSessionStatus.Unavailable,
        _ => ProductSessionStatus.InvalidRequest,
    };

    private static bool ValidText(string value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength;
}

internal static class DemoIdentityBootstrap
{
    public static async Task EnsureDemoAccountsAsync(
        LocalAccountService accounts,
        ILocalAccountStore store,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
            throw new ArgumentException("Le mot de passe du profil demo doit contenir au moins 12 caracteres.", nameof(password));

        await EnsureAsync("demo.consultation", "consultation", "Consultation", ["magazine.read"], cancellationToken);
        await EnsureAsync("demo.operateur", "operateur", "Opérateur", [
            "magazine.read", "tool.wear.edit", "tool.spindle.edit", "tool.prepare", "tool.load", "operation.read.own"], cancellationToken);
        await EnsureAsync("demo.regleur", "regleur", "Régleur outils", [
            "magazine.read", "tool.wear.edit", "tool.spindle.edit", "tool.prepare", "tool.load", "operation.read.own",
            "tool.data.edit", "tool.correctors.edit"], cancellationToken);
        await EnsureAsync("demo.admin", "admin", "Administrateur", [
            "magazine.read", "maintenance.read", "operation.read.machine", "audit.read", "audit.export", "identity.manage",
            "roles.manage", "license.install", "deployment.manage", "audit.policy.manage"], cancellationToken);

        async Task EnsureAsync(
            string subject,
            string userName,
            string displayName,
            string[] permissions,
            CancellationToken token)
        {
            var normalized = LocalAccountService.NormalizeUserName(userName);
            if (await store.FindByNormalizedUserNameAsync(normalized, token).ConfigureAwait(false) is not null) return;

            var result = await accounts.CreateHumanAsync(
                new SubjectId(subject),
                userName,
                displayName,
                password,
                permissions.Select(static value => new PermissionId(value)).ToArray(),
                token).ConfigureAwait(false);
            if (result.Status is not (LocalAccountCreateStatus.Created or LocalAccountCreateStatus.UserNameConflict))
                throw new InvalidOperationException($"Impossible de creer le compte demo '{userName}': {result.Status}.");
        }
    }
}
