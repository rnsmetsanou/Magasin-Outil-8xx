using MagasinOutil.Core;

namespace MagasinOutil.Transport;

public sealed class RemoteMagazineService : IIntegratedMagazineService
{
    private readonly IProductMagazineReadService _reader;
    private readonly IProductMagazineCommandService? _commands;
    private readonly IProductLicenseReadService? _licenses;
    private readonly string _sessionReference;
    private readonly string _clientId;
    private readonly object _gate = new();
    private IReadOnlyList<Location> _locations = [];
    private ProductPlatformStatus _status = new(false, null, null, null, null, null, "Core non lu.");
    private ProductLicenseView? _licenseStatus;
    private ProductCommandResult? _lastCommand;

    public RemoteMagazineService(IProductMagazineReadService reader, string sessionReference, string clientId)
        : this(reader, null, null, sessionReference, clientId)
    {
    }

    public RemoteMagazineService(
        IProductMagazineReadService reader,
        IProductMagazineCommandService? commands,
        IProductLicenseReadService? licenses,
        string sessionReference,
        string clientId)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _commands = commands;
        _licenses = licenses;
        _sessionReference = string.IsNullOrWhiteSpace(sessionReference) ? throw new ArgumentException("Session required.", nameof(sessionReference)) : sessionReference;
        _clientId = string.IsNullOrWhiteSpace(clientId) ? throw new ArgumentException("Client id required.", nameof(clientId)) : clientId;
    }

    public ProductPlatformStatus PlatformStatus
    {
        get { lock (_gate) return _status; }
    }

    public ProductLicenseView? LicenseStatus
    {
        get { lock (_gate) return _licenseStatus; }
    }

    public ProductCommandResult? LastCommand
    {
        get { lock (_gate) return _lastCommand; }
    }

    public IReadOnlyList<Location> Read()
    {
        lock (_gate) return _locations.ToArray();
    }

    public async ValueTask RefreshAsync(CancellationToken cancellationToken = default)
    {
        var result = await _reader.ReadAsync(
            new ProductMagazineReadRequest(
                ProductMagazineReadContract.Version,
                _sessionReference,
                _clientId),
            cancellationToken).ConfigureAwait(false);

        ProductLicenseView? license = null;
        if (_licenses is not null)
        {
            license = await _licenses.ReadLicenseAsync(
                new ProductSessionRequest(ProductSessionContract.Version, _sessionReference, _clientId),
                cancellationToken).ConfigureAwait(false);
        }

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

            if (license is not null) _licenseStatus = license;
        }
    }

    public EditResult Apply(EditTool edit)
    {
        if (_commands is null)
            return new(EditOutcome.Rejected, "Modification gouvernée non raccordée dans ce profil.");

        return Execute(new ProductCommandRequest(
            ProductCommandContract.Version,
            _sessionReference,
            _clientId,
            Guid.NewGuid().ToString(),
            ProductCommandKind.EditTool,
            edit.Location,
            edit.ToolId,
            edit.ExpectedRevision,
            Name: edit.Name,
            Wear: edit.Wear,
            Length: edit.Length));
    }

    public EditResult Transfer(SimulatedTransfer request)
    {
        if (_commands is null)
            return new(EditOutcome.Rejected, "Commande gouvernée non raccordée dans ce profil.");

        var kind = request.Destination switch
        {
            ToolPosition.Prepared => ProductCommandKind.PrepareTool,
            ToolPosition.Spindle => ProductCommandKind.LoadTool,
            _ => (ProductCommandKind?)null,
        };
        if (kind is null)
            return new(EditOutcome.Rejected, "Destination non prise en charge.");

        return Execute(new ProductCommandRequest(
            ProductCommandContract.Version,
            _sessionReference,
            _clientId,
            Guid.NewGuid().ToString(),
            kind.Value,
            request.Location,
            request.ToolId,
            request.ExpectedRevision,
            request.ExpectedOccupantId,
            request.ExpectedOccupantRevision));
    }

    private EditResult Execute(ProductCommandRequest request)
    {
        ProductCommandResult result;
        try
        {
            result = _commands!.ExecuteAsync(request).AsTask().GetAwaiter().GetResult();
        }
        catch
        {
            return new(EditOutcome.Rejected, "CoreHost indisponible pendant la commande.");
        }

        lock (_gate) _lastCommand = result;
        if (result.IsCompleted)
        {
            try
            {
                RefreshAsync().AsTask().GetAwaiter().GetResult();
            }
            catch
            {
                // The admitted effect result remains authoritative; the visual refresh can recover on the timer.
            }
        }

        var trace = result.OperationId is null
            ? $"Intent {Short(result.IntentId)}"
            : $"Intent {Short(result.IntentId)} · Op {Short(result.OperationId)}";
        var message = $"{result.Message} · {trace}";
        return result.Status switch
        {
            ProductCommandStatus.Completed => new(EditOutcome.AppliedInSimulation, message),
            ProductCommandStatus.Conflict => new(EditOutcome.Conflict, message),
            _ => new(EditOutcome.Rejected, message),
        };
    }

    private static string Short(string value) => value.Length <= 8 ? value : value[..8];
}
