using MagasinOutil.Core;

namespace MagasinOutil.CoreHost;

internal sealed record DemoProfileDefinition(
    string SubjectId,
    string UserName,
    string DisplayName,
    string Role,
    string[] Permissions);

internal static class DemoProfileCatalog
{
    public static IReadOnlyList<DemoProfileDefinition> Profiles { get; } =
    [
        new("demo.consultation", "consultation", "Consultation", "Consultation", ["magazine.read"]),
        new("demo.operateur", "operateur", "Opérateur", "Opérateur", [
            "magazine.read", "tool.wear.edit", "tool.spindle.edit", "tool.prepare", "tool.load", "operation.read.own"]),
        new("demo.regleur", "regleur", "Régleur outils", "Régleur outils", [
            "magazine.read", "tool.wear.edit", "tool.spindle.edit", "tool.prepare", "tool.load", "operation.read.own",
            "tool.data.edit", "tool.correctors.edit"]),
        new("demo.admin", "admin", "Administrateur", "Administrateur", [
            "magazine.read", "maintenance.read", "operation.read.machine", "audit.read", "audit.export", "identity.manage",
            "roles.manage", "license.install", "deployment.manage", "audit.policy.manage"]),
    ];

    public static IReadOnlyCollection<ProductUserProfileView> ToViews() => Profiles
        .Select(static profile => new ProductUserProfileView(
            profile.UserName,
            profile.DisplayName,
            profile.Role,
            true,
            profile.Permissions.OrderBy(static permission => permission, StringComparer.Ordinal).ToArray()))
        .ToArray();
}
