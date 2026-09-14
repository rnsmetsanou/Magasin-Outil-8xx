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
