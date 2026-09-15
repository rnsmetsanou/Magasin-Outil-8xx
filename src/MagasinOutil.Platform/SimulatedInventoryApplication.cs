using System.Globalization;
using MagasinOutil.Core;
using Platform.Poc.Application.Contracts;
using Platform.Poc.Foundation.Diagnostics;
using Platform.Poc.Foundation.Execution;
using Platform.Poc.Foundation.Time;
using Platform.Poc.Machine.Contracts.Connectivity;
using Platform.Poc.Machine.Contracts.Observation.Freshness;
using Platform.Poc.Machine.Contracts.Observation.Quality;
using Platform.Poc.Machine.Runtime.Connectivity;
using Platform.Poc.Technology.Simulator;

namespace MagasinOutil.Platform;

/// <summary>8xx composition: platform owns connectivity; the pilot owns rack/inventory rules.</summary>
public sealed class SimulatedInventoryApplication : IToolInventoryReader, IAsyncDisposable
{
    public const string Target = "magasin-8xx-simulator";
    private readonly SimulatedMagazine _magazine = new();
    private readonly SessionCoordinator _sessions;
    private readonly RuntimeIdentity _identity;

    private SimulatedInventoryApplication(SessionCoordinator sessions, RuntimeIdentity identity)
    {
        _sessions = sessions;
        _identity = identity;
    }

    public static async Task<SimulatedInventoryApplication> StartAsync(CancellationToken cancellationToken = default)
    {
        var target = new TargetId(Target);
        var endpoint = new EndpointId("magasin-8xx-simulation");
        var epoch = RuntimeEpoch.New();
        var technology = new SimulatorTechnology(target, endpoint);
        var sessions = new SessionCoordinator(technology.Connectivity, new SystemWallClock(),
            new SystemMonotonicClock(), NullDiagnosticSink.Instance, RunId.New(), epoch);
        var session = await sessions.ConnectAsync(target, endpoint, cancellationToken).ConfigureAwait(false);
        return new(sessions, new RuntimeIdentity(target.Value, epoch.ToString(), session.SessionId.ToString(), session.Generation.Value));
    }

    public ValueTask<ToolInventorySnapshot> ReadAsync(ToolInventoryReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.ContractVersion != ToolInventoryContract.Version)
            throw new InventoryReadException(InventoryReadFailure.Incompatible, "Version incompatible.");
        if (request.TargetId != Target)
            throw new InventoryReadException(InventoryReadFailure.WrongTarget, "Cible inconnue.");
        if (_sessions.State != ConnectionState.Connected)
            throw new InventoryReadException(InventoryReadFailure.Unavailable, "Session indisponible.");

        var places = _magazine.Read().Select(location => new ToolPlace(
            location.Number.ToString(CultureInfo.InvariantCulture), location.Rack.ToString(CultureInfo.InvariantCulture),
            location.Forbidden, location.Blocked, location.Present,
            location.Tool is { } tool ? new ToolInventoryItem(tool.Id, tool.Name,
                tool.Position.ToString(), tool.Condition.ToString(), tool.Revision,
                [new("Length", tool.Length, "mm"), new("LengthWear", tool.Wear, "mm")]) : null)).ToArray();

        return ValueTask.FromResult(new ToolInventorySnapshot(ToolInventoryContract.Version, _identity,
            new(SignalQuality.Good, SignalFreshness.Fresh, DateTimeOffset.UtcNow, null, "Magasin8xx.Simulation"),
            "AtomicSimulationSnapshot", [ToolInventoryContract.ReadCapability], places));
    }

    public EditResult? ValidateTransfer(SimulatedTransfer request) => _magazine.ValidateTransfer(request);
    public EditResult Transfer(SimulatedTransfer request) => _magazine.Transfer(request);
    public EditResult? ValidateEdit(EditTool request) => _magazine.ValidateEdit(request);
    public EditResult Apply(EditTool request) => _magazine.Apply(request);

    public async ValueTask DisposeAsync()
    {
        if (_sessions.State == ConnectionState.Connected)
            await _sessions.DisconnectAsync().ConfigureAwait(false);
    }
}
