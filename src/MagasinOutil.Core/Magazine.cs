namespace MagasinOutil.Core;

public enum ToolPosition { Magazine, Spindle, Prepared }
public enum ToolCondition { Available, Defective, EndOfLife }
public sealed record Tool(int Id, string Name, ToolPosition Position, ToolCondition Condition,
    decimal Length, decimal Wear, long Revision);
public sealed record Location(int Number, int Rack, bool Forbidden, bool Blocked, Tool? Tool)
{
    public bool Present => Tool?.Position == ToolPosition.Magazine;
}
public sealed record EditTool(int Location, int ToolId, long ExpectedRevision, string Name, decimal Wear);
public sealed record SimulatedTransfer(int Location, int ToolId, long ExpectedRevision,
    ToolPosition Destination, int? ExpectedOccupantId, long? ExpectedOccupantRevision);
public enum EditOutcome { AppliedInSimulation, Rejected, Conflict }
public sealed record EditResult(EditOutcome Outcome, string Message);

// This first contract deliberately exposes no ADS symbols or vendor types.
// A live asynchronous adapter will require observed quality, freshness and operation tracking.
public interface IMagazineService
{
    IReadOnlyList<Location> Read();
    EditResult Apply(EditTool edit);
    EditResult Transfer(SimulatedTransfer request);
}

public sealed class SimulatedMagazine : IMagazineService
{
    private readonly object _gate = new();
    private readonly List<Location> _locations;
    public SimulatedMagazine()
    {
        int[] empty = [7,18,25,31,53,77,83,97,109,119,133];
        _locations = Enumerable.Range(1, 140).Select(n =>
        {
            var forbidden = n is 1 or 2 or 96;
            var blocked = n is 46 or 69;
            var rack = n <= 95 ? (n - 1) / 19 + 1 : (n - 96) / 15 + 6;
            Tool? tool = forbidden || blocked || empty.Contains(n) ? null : new(
                n is 12 or 34 ? n : 100 + n,
                n == 34 ? "Foret Ø 6" : "Fraise de finition Ø 8",
                n == 12 ? ToolPosition.Spindle : n == 34 ? ToolPosition.Prepared : ToolPosition.Magazine,
                n == 58 ? ToolCondition.Defective : n == 112 ? ToolCondition.EndOfLife : ToolCondition.Available,
                142.5m + n / 10m, -0.02m, 1);
            return new Location(n, rack, forbidden, blocked, tool);
        }).ToList();
    }
    public IReadOnlyList<Location> Read() { lock (_gate) return _locations.ToArray(); }
    public EditResult Transfer(SimulatedTransfer request)
    {
        lock (_gate)
        {
            if (request.Destination is not (ToolPosition.Prepared or ToolPosition.Spindle))
                return new(EditOutcome.Rejected, "Destination non prise en charge.");
            var index = _locations.FindIndex(l => l.Number == request.Location);
            if (index < 0 || _locations[index] is { Forbidden: true } or { Blocked: true } || _locations[index].Tool is null)
                return new(EditOutcome.Rejected, "Emplacement indisponible.");
            var location = _locations[index];
            var tool = location.Tool!;
            if (tool.Id != request.ToolId || tool.Revision != request.ExpectedRevision)
                return new(EditOutcome.Conflict, "L’outil a changé. Relire avant de confirmer.");
            if (tool.Condition != ToolCondition.Available || tool.Position == request.Destination ||
                (request.Destination == ToolPosition.Prepared && tool.Position != ToolPosition.Magazine))
                return new(EditOutcome.Rejected, "Action indisponible pour cet outil dans le simulateur.");
            var occupantIndex = _locations.FindIndex(l => l.Tool?.Position == request.Destination);
            var occupant = occupantIndex < 0 ? null : _locations[occupantIndex].Tool;
            if (occupant?.Id != request.ExpectedOccupantId || occupant?.Revision != request.ExpectedOccupantRevision)
                return new(EditOutcome.Conflict, "L’occupation de destination a changé. Relire avant de confirmer.");
            // A simulation transaction only: this is NOT a physical movement/completion protocol.
            if (occupantIndex >= 0)
                _locations[occupantIndex] = _locations[occupantIndex] with
                { Tool = occupant! with { Position = ToolPosition.Magazine, Revision = occupant!.Revision + 1 } };
            _locations[index] = location with { Tool = tool with { Position = request.Destination, Revision = tool.Revision + 1 } };
            return new(EditOutcome.AppliedInSimulation, $"Simulation confirmée : T{tool.Id} " +
                (request.Destination == ToolPosition.Spindle ? "en broche." : "préparé."));
        }
    }
    public EditResult Apply(EditTool edit)
    {
        lock (_gate)
        {
            var index = _locations.FindIndex(l => l.Number == edit.Location);
            if (index < 0 || _locations[index] is { Forbidden: true } or { Blocked: true } || _locations[index].Tool is null)
                return new(EditOutcome.Rejected, "Emplacement indisponible.");
            var location = _locations[index];
            var tool = location.Tool!;
            if (tool.Id != edit.ToolId || tool.Revision != edit.ExpectedRevision)
                return new(EditOutcome.Conflict, "Les données de l’outil ont changé. Annuler et relire la fiche.");
            var name = edit.Name.Trim();
            if (name.Length is < 1 or > 30)
                return new(EditOutcome.Rejected, "Le nom doit contenir de 1 à 30 caractères dans le simulateur.");
            if (decimal.Round(edit.Wear, 3) != edit.Wear)
                return new(EditOutcome.Rejected, "Trois décimales au maximum dans le simulateur.");
            _locations[index] = location with { Tool = tool with { Name = name, Wear = edit.Wear, Revision = tool.Revision + 1 } };
            return new(EditOutcome.AppliedInSimulation, $"Modification simulée confirmée pour T{tool.Id}.");
        }
    }
}
