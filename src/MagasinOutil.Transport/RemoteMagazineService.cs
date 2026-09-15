using MagasinOutil.Core;

namespace MagasinOutil.Transport;

public sealed class RemoteMagazineService(
    IProductMagazineReadService reader,
    string sessionReference,
    string clientId) : IIntegratedMagazineService
{
    private readonly object _gate = new();
    private IReadOnlyList<Location> _locations = [];
    private ProductPlatformStatus _status = new(false, null, null, null, null, null, "Core non lu.");

    public ProductPlatformStatus PlatformStatus
    {
        get { lock (_gate) return _status; }
    }

    public IReadOnlyList<Location> Read()
    {
        lock (_gate) return _locations.ToArray();
    }

    public async ValueTask RefreshAsync(CancellationToken cancellationToken = default)
    {
        var result = await reader.ReadAsync(
            new ProductMagazineReadRequest(
                ProductMagazineReadContract.Version,
                sessionReference,
                clientId),
            cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            if (result.IsSuccess && result.Snapshot is not null)
            {
                _locations = result.Snapshot.Locations.ToArray();
                var observation = result.Snapshot.Observation;
                _status = new ProductPlatformStatus(
                    true,
                    observation.Quality,
                    observation.Freshness,
                    observation.SessionGeneration,
                    observation.Origin,
                    observation.ObservedAt,
                    "Core connecté.");
            }
            else
            {
                _status = _status with
                {
                    Connected = false,
                    Message = result.Reason,
                };
            }
        }
    }

    public EditResult Apply(EditTool edit) =>
        new(EditOutcome.Rejected, "Modification gouvernée disponible à partir de D3 ; aucune mutation locale n'est effectuée.");

    public EditResult Transfer(SimulatedTransfer request) =>
        new(EditOutcome.Rejected, "Commande gouvernée disponible à partir de D3 ; aucune mutation locale n'est effectuée.");
}
