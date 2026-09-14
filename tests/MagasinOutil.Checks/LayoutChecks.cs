using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MagasinOutil.Core;
using MagasinOutil.Desktop;

internal static class LayoutChecks
{
    public static void Run()
    {
        AppBuilder.Configure<App>().UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
        // Reserve 28 vertical pixels for Windows decorations at the minimum physical size.
        foreach (var size in new[] { new Size(1024, 740), new Size(1920, 1052) })
        {
            var window = new MainWindow(new SimulatedMagazine()) { MinHeight = 0, Width = size.Width, Height = size.Height };
            window.Show();
            foreach (var theme in new[] { "Clair", "Sombre" })
            {
                Click(window, theme);
                for (var rack = 1; rack <= 8; rack++)
                {
                    Click(window, "Rack " + rack); Verify(window);
                    var racks = Buttons(window).Where(b => Text(b).StartsWith("Rack ")).ToArray();
                    if (racks.Length != 8 || racks.Any(b => Math.Abs(b.Bounds.Height - 52) > 1 || Math.Abs(b.Bounds.Width - 88) > 1))
                        throw new Exception("Dimensions des racks incohérentes");
                }
                Click(window, "Rack 3"); Click(window, "39 ●"); Click(window, "Correcteurs"); Verify(window);
                Click(window, "Modifier les données"); Verify(window);
                var fields = window.GetVisualDescendants().OfType<TextBox>().Where(Visible).ToArray();
                if (fields.Length != 3) throw new Exception("Trois champs attendus dans l’éditeur");
                fields[1].Text = "155,250";
                Click(window, theme == "Clair" ? "Sombre" : "Clair"); Verify(window);
                if (!window.GetVisualDescendants().OfType<TextBox>().Where(Visible).Any(t => t.Text == "155,250"))
                    throw new Exception("Brouillon perdu au changement de thème");
                Click(window, "Vérifier"); Verify(window); Click(window, "Confirmer la simulation"); Verify(window);
                Click(window, "Préparer"); Verify(window); Click(window, "Annuler");
                var search = window.GetVisualDescendants().OfType<TextBox>().Single(Visible);
                search.Text = "Fraise"; Verify(window); Click(window, "Suivant"); Verify(window);
                search.Text = "";
                Console.WriteLine($"OK — Placement {size.Width} × {size.Height}, {theme}, racks/recherche/édition/actions");
            }
            window.Close();
        }
    }
    private static bool Visible(Control c) => c.IsVisible && c.GetVisualAncestors().OfType<Control>().All(a => a.IsVisible);
    private static IEnumerable<Button> Buttons(Window w) => w.GetVisualDescendants().OfType<Button>().Where(Visible);
    private static string Text(Button b) => b.Content is string s ? s :
        b.Content is TextBlock t ? t.Text ?? "" : b.Content is Control c ?
        string.Join(" ", c.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text)) : "";
    private static void Click(Window w, string label)
    {
        Dispatcher.UIThread.RunJobs();
        var b = Buttons(w).Single(b => Text(b) == label || Text(b).StartsWith(label + " "));
        if (!b.IsEnabled) throw new Exception("Action désactivée : " + label);
        b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
    }
    private static void Verify(Window w)
    {
        Dispatcher.UIThread.RunJobs();
        foreach (var panel in w.GetVisualDescendants().OfType<Border>().Where(p => p.Name is "MagazinePanel" or "ToolPanel").Where(Visible))
        {
            foreach (var c in panel.GetVisualDescendants().OfType<Control>().Where(c => c is Button or TextBlock).Where(Visible))
            {
                var point = c.TranslatePoint(new Point(), panel) ?? throw new Exception("Contrôle sans parent");
                if (point.Y < 0 || point.Y + c.Bounds.Height > panel.Bounds.Height - 4 || point.X < 0 || point.X + c.Bounds.Width > panel.Bounds.Width + 1)
                    throw new Exception($"Débordement {panel.Name}: {c.GetType().Name} {(c is TextBlock t ? t.Text : c.Name)} {point} {c.Bounds.Size}, panneau {panel.Bounds.Size}");
                if (c is Button && c.Bounds.Height < 44) throw new Exception("Cible tactile trop petite");
            }
        }
        var layout = w.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "ToolLayout");
        var content = layout.Children[0]; var actions = layout.Children[1];
        if (content.Bounds.Bottom > actions.Bounds.Top + 1) throw new Exception("Chevauchement fiche/actions");
    }
}
