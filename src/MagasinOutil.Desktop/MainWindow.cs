using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using MagasinOutil.Core;

namespace MagasinOutil.Desktop;

// Code-built Avalonia view. Business decisions stay behind IMagazineService.
public sealed class MainWindow : Window
{
    private readonly IMagazineService _service;
    private readonly Bitmap _blueLogo = new(AssetLoader.Open(new Uri("avares://MagasinOutil.Desktop/Assets/wm-logo-blue.png")));
    private readonly Bitmap _whiteLogo = new(AssetLoader.Open(new Uri("avares://MagasinOutil.Desktop/Assets/wm-logo-white.png")));
    private readonly Grid _shell = new() { RowDefinitions = new("Auto,Auto,*,Auto") };
    private readonly Grid _body = new() { ColumnDefinitions = new("1.2*,*") };
    private readonly StackPanel _left = new() { Spacing = 10 };
    private readonly StackPanel _detail = new() { Spacing = 10 };
    private readonly TextBlock _notice = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Image _logo = new() { Width = 120, Height = 48, Stretch = Stretch.Uniform };
    private readonly TextBox _search = new() { PlaceholderText = "Nom, T12 ou place 27", MinHeight = 44 };
    private readonly StackPanel _rackArea = new() { Spacing = 8 };
    private readonly WrapPanel _slots = new() { Orientation = Orientation.Horizontal };
    private readonly TextBlock _rackTitle = new();
    private readonly Button _spindle;
    private readonly Button _prepared;
    private int _selected = 27, _rack = 2, _page;
    private bool _dark, _review, _offsets;
    private EditTool? _draft;
    private string _draftName = "", _draftWear = "";
    private TextBox? _wearInput;
    private readonly List<Border> _panels = [];
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("fr-CH");
    private IBrush Ink => Brush(_dark ? "#FFFFFF" : "#202945");
    private IBrush Panel => Brush(_dark ? "#202945" : "#FFFFFF");
    private IBrush Back => Brush(_dark ? "#051C2C" : "#F1F2F5");
    private IBrush Selected => Brush(_dark ? "#52658E" : "#E0E3ED");
    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));

    public MainWindow(IMagazineService service)
    {
        _service = service;
        Title = "Gestion des outils — WM — Simulation";
        Width = 1024; Height = 768; MinWidth = 1024; MinHeight = 768;
        CanResize = true;
        FontSize = 16;
        // Poppins is used when bundled; this explicit fallback keeps offline startup usable.
        FontFamily = AssetLoader.Exists(new Uri("avares://MagasinOutil.Desktop/Assets/Poppins-Regular.ttf"))
            ? new FontFamily("avares://MagasinOutil.Desktop/Assets#Poppins") : Avalonia.Media.FontFamily.Default;
        var header = new Grid { ColumnDefinitions = new("Auto,*,Auto"), Margin = new(16, 8) };
        // The clear space is based on the original monogram/name separation (X).
        var logoSpace = new Border { Child = _logo, Padding = new(20, 14), Margin = new(0, 0, 12, 0) };
        Put(header, logoSpace, 0, 0);
        var title = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 3 };
        title.Children.Add(Label("Gestion des outils", 23));
        title.Children.Add(Label("Plateforme Multi-Technologie · Données simulées", 13));
        Put(header, title, 0, 1);
        var themes = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        themes.Children.Add(Action("Clair", () => ApplyTheme(false)));
        themes.Children.Add(Action("Sombre", () => ApplyTheme(true)));
        Put(header, themes, 0, 2);
        Put(_shell, header, 0, 0);
        var current = new Grid { ColumnDefinitions = new("*,*"), Margin = new(16, 0, 16, 10) };
        _spindle = Action("", () => Select(12));
        _prepared = Action("", () => Select(34));
        _spindle.Margin = new(0, 0, 6, 0); _prepared.Margin = new(6, 0, 0, 0);
        Put(current, _spindle, 0, 0); Put(current, _prepared, 0, 1);
        Put(_shell, current, 1, 0);
        _body.Margin = new(16, 0, 16, 10);
        var leftPanel = Surface(_left); leftPanel.Margin = new(0, 0, 6, 0);
        var rightPanel = Surface(_detail); rightPanel.Margin = new(6, 0, 0, 0);
        Put(_body, leftPanel, 0, 0); Put(_body, rightPanel, 0, 1);
        Put(_shell, _body, 2, 0);
        var footer = new Grid { ColumnDefinitions = new("*,Auto"), Margin = new(16, 0, 16, 10) };
        Put(footer, _notice, 0, 0); Put(footer, Label("Simulation · aucune liaison machine", 12), 0, 1);
        Put(_shell, footer, 3, 0);
        _left.Children.Add(Label("Magasin · 137 places physiques", 18));
        _left.Children.Add(_search); _left.Children.Add(_rackArea);
        _left.Children.Add(_rackTitle); _left.Children.Add(_slots);
        _left.Children.Add(Label("● Présent   ↗ Hors magasin   ! À contrôler   × Indisponible   — Vide", 12));
        _search.TextChanged += (_, _) => { _page = 0; RenderLocations(); };
        KeyDown += (_, e) => { if (e.Key == Key.F11) { WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen; e.Handled = true; } };
        Closed += (_, _) => { _blueLogo.Dispose(); _whiteLogo.Dispose(); };
        Content = _shell;
        ApplyTheme(false);
    }
    private static void Put(Grid parent, Control child, int row, int column)
    { Grid.SetRow(child, row); Grid.SetColumn(child, column); parent.Children.Add(child); }
    private static TextBlock Label(string text, double size = 16) => new()
    { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap };
    private static Button Action(string text, Action click)
    {
        var button = new Button { Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, MinHeight = 44,
            Padding = new(12, 6), HorizontalContentAlignment = HorizontalAlignment.Center };
        button.Click += (_, _) => click(); return button;
    }
    private Border Surface(Control child)
    {
        var border = new Border { Padding = new(12), CornerRadius = new(8), Child = new ScrollViewer { Content = child,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled } };
        _panels.Add(border); return border;
    }
    private void ApplyTheme(bool dark)
    {
        _dark = dark; RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        Background = Back; Foreground = Ink;
        foreach (var panel in _panels) panel.Background = Panel;
        _logo.Source = dark ? _whiteLogo : _blueLogo;
        Render();
    }
    private static string Number(decimal value) => value.ToString("F3", Culture);
    private static string PositionLabel(ToolPosition position) => position switch
    { ToolPosition.Spindle => "En broche", ToolPosition.Prepared => "Préparé (simulation)", _ => "Dans le magasin" };
    private static string State(MagasinOutil.Core.Location l) => l.Forbidden ? "Interdit" : l.Blocked ? "Bloqué" : l.Tool is null ? "Vide" :
        l.Tool.Condition == ToolCondition.Defective ? "Défectueux" : l.Tool.Condition == ToolCondition.EndOfLife ? "Fin de vie" :
        l.Present ? "Présent" : PositionLabel(l.Tool.Position);
    private void Select(int location)
    {
        if (_draft is not null) return;
        var item = _service.Read().Single(l => l.Number == location);
        _selected = location; _rack = item.Rack; _notice.Text = ""; Render();
    }
    private void Render()
    {
        var locations = _service.Read();
        foreach (var pair in new[] { (_spindle, 12, "En broche"), (_prepared, 34, "Préparé") })
        {
            var tool = locations.Single(l => l.Number == pair.Item2).Tool!;
            pair.Item1.Content = $"{pair.Item3} · T{tool.Id} · place fixe {pair.Item2}";
            pair.Item1.IsEnabled = _draft is null;
        }
        _search.IsEnabled = _draft is null;
        RenderLocations(); RenderDetail();
    }
    private void RenderLocations()
    {
        var locations = _service.Read(); _rackArea.Children.Clear(); _slots.Children.Clear();
        var query = (_search.Text ?? "").Trim();
        if (query.Length > 0)
        {
            var found = locations.Where(l => l.Number.ToString() == query || l.Tool is { } t &&
                (t.Id.ToString() == query || $"T{t.Id}".Equals(query, StringComparison.OrdinalIgnoreCase) ||
                 t.Name.Contains(query, StringComparison.OrdinalIgnoreCase))).ToArray();
            _page = Math.Clamp(_page, 0, Math.Max(0, (found.Length - 1) / 4));
            _rackArea.Children.Add(Label($"{found.Length} résultat(s) · page {_page + 1}/{Math.Max(1, (found.Length + 3) / 4)}", 13));
            foreach (var l in found.Skip(_page * 4).Take(4))
            {
                var button = Action($"Place {l.Number} · " + (l.Tool is {} t ? $"T{t.Id} · {t.Name}" : State(l)), () => Select(l.Number));
                button.IsEnabled = _draft is null; _rackArea.Children.Add(button);
            }
            var pages = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var prev = Action("Précédent", () => { _page--; RenderLocations(); }); prev.IsEnabled = _page > 0 && _draft is null;
            var next = Action("Suivant", () => { _page++; RenderLocations(); }); next.IsEnabled = (_page + 1) * 4 < found.Length && _draft is null;
            pages.Children.Add(prev); pages.Children.Add(next); _rackArea.Children.Add(pages);
        }
        else
        {
            foreach (var group in new[] { new[] {5,4,3,2,1}, new[] {6,7,8} })
            {
                _rackArea.Children.Add(Label(group[0] == 5 ? "Partie supérieure" : "Partie inférieure", 13));
                var row = new WrapPanel();
                foreach (var rack in group)
                {
                    var button = Action($"Rack {rack}", () => Select(locations.First(l => l.Rack == rack).Number));
                    button.Width = 84; button.Margin = new(0, 0, 5, 5); button.IsEnabled = _draft is null;
                    if (rack == _rack) { button.Background = Selected; button.Foreground = Ink; }
                    row.Children.Add(button);
                }
                _rackArea.Children.Add(row);
            }
        }
        var rackItems = locations.Where(l => l.Rack == _rack).ToArray();
        _rackTitle.Text = $"Rack {_rack} · places {rackItems.First().Number}–{rackItems.Last().Number}";
        foreach (var l in rackItems)
        {
            var mark = l.Forbidden || l.Blocked ? "×" : l.Tool is null ? "—" : l.Tool.Condition != ToolCondition.Available ? "!" : l.Present ? "●" : "↗";
            var button = Action($"{l.Number}  {mark}", () => Select(l.Number));
            button.Width = 70; button.MinHeight = 48; button.Margin = new(0, 0, 5, 5); button.IsEnabled = _draft is null;
            Avalonia.Automation.AutomationProperties.SetName(button, $"Place {l.Number}, {State(l)}");
            if (_selected == l.Number) { button.Background = Selected; button.Foreground = Ink; }
            _slots.Children.Add(button);
        }
    }
    private void RenderDetail()
    {
        _detail.Children.Clear(); _wearInput = null;
        var location = _service.Read().Single(l => l.Number == _selected);
        _detail.Children.Add(Label($"PLACE {location.Number} · RACK {location.Rack} · {State(location)}", 13));
        if (location.Tool is not { } tool) { _detail.Children.Add(Label("Aucun outil modifiable à cet emplacement.")); return; }
        _detail.Children.Add(Label($"Outil T{tool.Id}", 22));
        if (_draft is not null)
        {
            if (_review)
            {
                _detail.Children.Add(Label("Vérifier la modification", 18));
                _detail.Children.Add(Label($"Nom : {tool.Name} → {_draft.Name}"));
                _detail.Children.Add(Label($"Usure : {Number(tool.Wear)} → {Number(_draft.Wear)} mm"));
                _detail.Children.Add(Label("Application simulée à cet outil uniquement.", 13));
                _detail.Children.Add(Action("Retour", () => { _review = false; RenderDetail(); }));
                _detail.Children.Add(Action("Confirmer la simulation", Apply));
                return;
            }
            _detail.Children.Add(Label("Nom de l’outil", 13));
            var name = new TextBox { Text = _draftName, MaxLength = 30, MinHeight = 44 };
            name.TextChanged += (_, _) => _draftName = name.Text ?? ""; _detail.Children.Add(name);
            _detail.Children.Add(Label("Usure longueur · mm (valeur fictive)", 13));
            _wearInput = new TextBox { Text = _draftWear, MinHeight = 44 };
            _wearInput.TextChanged += (_, _) => _draftWear = _wearInput?.Text ?? _draftWear;
            _detail.Children.Add(_wearInput);
            var pad = new Grid { ColumnDefinitions = new("*,*,*,*"), RowDefinitions = new("Auto,Auto,Auto,Auto") };
            string[] keys = ["7","8","9","⌫","4","5","6","±","1","2","3","Effacer","0",","];
            for (var i = 0; i < keys.Length; i++)
            { var key = keys[i]; var button = Action(key, () => Keypad(key)); button.Margin = new(2); button.HorizontalAlignment = HorizontalAlignment.Stretch; Put(pad, button, i / 4, i % 4); }
            _detail.Children.Add(pad);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            actions.Children.Add(Action("Annuler", () => { _draft = null; _notice.Text = "Modification annulée."; Render(); }));
            actions.Children.Add(Action("Vérifier", Review)); _detail.Children.Add(actions); return;
        }
        _detail.Children.Add(Label(tool.Name, 18));
        _detail.Children.Add(Label($"Place fixe : {location.Number}\nPosition actuelle : {PositionLabel(tool.Position)}\nPrésence à la place : {(location.Present ? "Oui" : "Non")}"));
        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        tabs.Children.Add(Action("Données", () => { _offsets = false; RenderDetail(); }));
        tabs.Children.Add(Action("Correcteurs", () => { _offsets = true; RenderDetail(); })); _detail.Children.Add(tabs);
        _detail.Children.Add(Label($"Longueur : {Number(tool.Length)} mm"));
        if (_offsets) _detail.Children.Add(Label($"Usure longueur : {Number(tool.Wear)} mm\nCorrecteur de fraisage fictif n° 1"));
        _detail.Children.Add(Action("Modifier les données", () =>
        {
            _draft = new(location.Number, tool.Id, tool.Revision, tool.Name, tool.Wear);
            _draftName = tool.Name; _draftWear = Number(tool.Wear); _review = false; _notice.Text = "Brouillon · aucune écriture machine"; Render();
        }));
    }
    private void Keypad(string key)
    {
        if (_wearInput is null) return;
        var text = _wearInput.Text ?? "";
        _wearInput.Text = key switch
        {
            "Effacer" => "", "⌫" => text.Length == 0 ? "" : text[..^1],
            "±" => text.StartsWith('-') ? text[1..] : "-" + text,
            "," => text.Contains(',') || text.Contains('.') ? text : text + ",", _ => text + key
        };
    }
    private void Review()
    {
        if (_draft is null) return;
        if (string.IsNullOrWhiteSpace(_draftName) || _draftName.Trim().Length > 30 ||
            !decimal.TryParse(_draftWear.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var wear) || decimal.Round(wear, 3) != wear)
        { _notice.Text = "Vérifier le nom et l’usure (trois décimales maximum)."; return; }
        _draft = _draft with { Name = _draftName.Trim(), Wear = wear }; _review = true; _notice.Text = ""; RenderDetail();
    }
    private void Apply()
    {
        if (_draft is null) return;
        var result = _service.Apply(_draft); _notice.Text = result.Message;
        if (result.Outcome == EditOutcome.AppliedInSimulation) { _draft = null; _review = false; }
        else _review = false;
        Render();
    }
}
