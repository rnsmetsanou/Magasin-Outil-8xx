using MagasinOutil.Core;
using Microsoft.Data.Sqlite;

namespace MagasinOutil.CoreHost;

internal sealed class DemoAdministrationApplication(
    DemoSessionApplication sessions,
    IProductLicenseReadService licenses,
    string admissionDatabasePath) : IProductAdministrationReadService
{
    private readonly string _admissionDatabasePath = Path.GetFullPath(admissionDatabasePath);

    public async ValueTask<ProductAdministrationSnapshot?> ReadAdministrationAsync(
        ProductSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var sessionResult = await sessions.ResolveAsync(request, cancellationToken).ConfigureAwait(false);
        if (!sessionResult.IsValid || sessionResult.Session is null)
            return null;

        var session = sessionResult.Session;
        var license = await licenses.ReadLicenseAsync(request, cancellationToken).ConfigureAwait(false);
        var canViewUsers = session.Permissions.Contains("identity.manage", StringComparer.Ordinal) ||
                           session.Permissions.Contains("roles.manage", StringComparer.Ordinal);
        var canViewAudit = session.Permissions.Contains("audit.read", StringComparer.Ordinal);

        var users = canViewUsers ? DemoProfileCatalog.ToViews() : [];
        var audit = canViewAudit
            ? await ReadAdmissionAuditAsync(cancellationToken).ConfigureAwait(false)
            : [];

        return new ProductAdministrationSnapshot(
            session,
            license,
            canViewUsers,
            users,
            canViewAudit,
            audit,
            "Audit durable des admissions de commandes",
            "Vue produit gouvernée par la session CoreHost.");
    }

    private async ValueTask<IReadOnlyCollection<ProductAuditEntryView>> ReadAdmissionAuditAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_admissionDatabasePath)) return [];

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _admissionDatabasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                a.event_id,
                a.intent_id,
                d.operation_id,
                a.recorded_at_utc,
                a.subject_id,
                a.client_id,
                a.target_id,
                a.action,
                a.decision,
                a.policy_revision
            FROM admission_audit a
            INNER JOIN durable_admissions d ON d.intent_id = a.intent_id
            ORDER BY a.recorded_at_utc DESC
            LIMIT 100;
            """;

        var entries = new List<ProductAuditEntryView>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            entries.Add(new ProductAuditEntryView(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                DateTimeOffset.Parse(reader.GetString(3), System.Globalization.CultureInfo.InvariantCulture),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetInt64(9)));
        }

        return entries;
    }
}
