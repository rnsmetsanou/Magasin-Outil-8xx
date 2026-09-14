using MagasinOutil.Core;
static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("OK — " + message);
}
var service = new SimulatedMagazine();
var snapshot = service.Read();
Check(snapshot.Count == 140 && snapshot.Count(l => !l.Forbidden) == 137, "140 positions, 137 autorisées");
Check(snapshot.Where(l => l.Forbidden).Select(l => l.Number).SequenceEqual(new[] { 1, 2, 96 }), "Positions interdites");
Check(snapshot.Single(l => l.Number == 96).Rack == 6 && snapshot.Last().Rack == 8, "Frontière des racks haut/bas");
var old = snapshot.Single(l => l.Number == 27).Tool!;
var edit = new EditTool(27, old.Id, old.Revision, "Outil modifié", .025m);
Check(service.Apply(edit).Outcome == EditOutcome.AppliedInSimulation, "Modification ciblée");
Check(service.Apply(edit).Outcome == EditOutcome.Conflict, "Double soumission refusée par révision");
Check(snapshot.Single(l => l.Number == 27).Tool == old, "Ancienne observation immuable");
Check(service.Read().Single(l => l.Number == 28) == snapshot.Single(l => l.Number == 28), "Outil voisin inchangé");
Check(service.Apply(edit with { ToolId = 999, ExpectedRevision = 2 }).Outcome == EditOutcome.Conflict, "Identité incorrecte refusée");
Check(service.Apply(edit with { ExpectedRevision = 2, Name = " " }).Outcome == EditOutcome.Rejected, "Validation côté service");
Check(service.Apply(edit with { Location = 1 }).Outcome == EditOutcome.Rejected, "Écriture sur place interdite refusée");
Check(!service.Read().Single(l => l.Number == 12).Present, "Place fixe distincte de la présence en broche");

// Transfers are atomic simulator operations, never a PLC movement protocol.
static SimulatedTransfer Request(SimulatedMagazine magazine, int place, ToolPosition destination)
{
    var view = magazine.Read();
    var tool = view.Single(l => l.Number == place).Tool!;
    var occupant = view.FirstOrDefault(l => l.Tool?.Position == destination)?.Tool;
    return new(place, tool.Id, tool.Revision, destination, occupant?.Id, occupant?.Revision);
}
var transfers = new SimulatedMagazine();
var initial = transfers.Read();
var prepare27 = Request(transfers, 27, ToolPosition.Prepared);
var staleDestination = Request(transfers, 28, ToolPosition.Prepared);
Check(transfers.Transfer(prepare27).Outcome == EditOutcome.AppliedInSimulation, "Préparation simulée confirmée");
var prepared = transfers.Read();
Check(prepared.Count(l => l.Tool?.Position == ToolPosition.Prepared) == 1 &&
    prepared.Single(l => l.Number == 27).Tool!.Position == ToolPosition.Prepared &&
    prepared.Single(l => l.Number == 34).Present, "Un seul préparé, ancien occupant rendu à sa place fixe");
Check(transfers.Transfer(prepare27).Outcome == EditOutcome.Conflict, "Rejeu de transfert refusé");
Check(transfers.Transfer(staleDestination).Outcome == EditOutcome.Conflict && transfers.Read().SequenceEqual(prepared),
    "Destination devenue obsolète : refus sans mutation partielle");
var load27 = Request(transfers, 27, ToolPosition.Spindle);
Check(transfers.Transfer(load27).Outcome == EditOutcome.AppliedInSimulation, "Chargement simulé de l’outil préparé");
var loaded = transfers.Read();
Check(loaded.Count(l => l.Tool?.Position == ToolPosition.Spindle) == 1 &&
    loaded.All(l => l.Tool?.Position != ToolPosition.Prepared) && loaded.Single(l => l.Number == 12).Present,
    "Broche unique, préparation libérée et ancien outil revenu au magasin");
Check(initial.Select(l => (l.Number, l.Tool?.Id)).SequenceEqual(loaded.Select(l => (l.Number, l.Tool?.Id))) &&
    loaded.Single(l => l.Number == 29) == initial.Single(l => l.Number == 29), "Affectations fixes conservées, autre outil inchangé");
Check(transfers.Transfer(Request(transfers, 27, ToolPosition.Prepared)).Outcome == EditOutcome.Rejected,
    "Préparation depuis la broche refusée");
Check(transfers.Transfer(Request(transfers, 58, ToolPosition.Spindle)).Outcome == EditOutcome.Rejected &&
    transfers.Transfer(Request(transfers, 112, ToolPosition.Prepared)).Outcome == EditOutcome.Rejected &&
    transfers.Read().SequenceEqual(loaded), "Défectueux et fin de vie refusés sans mutation");
var outdatedOccupant = Request(transfers, 28, ToolPosition.Spindle);
var spindleTool = loaded.Single(l => l.Number == 27).Tool!;
transfers.Apply(new(27, spindleTool.Id, spindleTool.Revision, "Révisé", spindleTool.Wear));
var revised = transfers.Read();
Check(transfers.Transfer(outdatedOccupant).Outcome == EditOutcome.Conflict && transfers.Read().SequenceEqual(revised),
    "Révision de l’occupant contrôlée même à identité constante");
Check(transfers.Transfer(Request(transfers, 28, ToolPosition.Prepared)).Outcome == EditOutcome.AppliedInSimulation,
    "Préparation vers une destination libre");
Check(transfers.Transfer(Request(transfers, 29, ToolPosition.Magazine)).Outcome == EditOutcome.Rejected &&
    transfers.Transfer(new(46, 146, 1, ToolPosition.Spindle, null, null)).Outcome == EditOutcome.Rejected,
    "Destination non prise en charge et place bloquée refusées");
