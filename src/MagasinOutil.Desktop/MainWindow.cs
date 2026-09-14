using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
    private readonly StackPanel _left = new() { Spacing = 6 };
    private readonly StackPanel _detail = new() { Spacing = 6 };
    private readonly TextBlock _notice = new() { TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly Image _logo = new() { Width = 156, Height = 62, Stretch = Stretch.Uniform };
    private readonly TextBox _search = new() { PlaceholderText = "Nom, T12 ou place 27", MinHeight = 44 };
    private readonly StackPanel _rackArea = new() { Spacing = 4 };
    private readonly UniformGrid _slots = new() { Columns = 6 };
    private readonly WrapPanel _legend = new();
    private readonly TextBlock _rackTitle = new();
    private readonly Button _spindle;
    private readonly Button _prepared;
    private int _selected = 27, _rack = 2, _page;
    private bool _dark, _review, _offsets;
    private EditTool? _draft;
    private SimulatedTransfer? _transfer;
    private bool IsBusy => _draft is not null || _transfer is not null;
    private string _draftName = "", _draftWear = "", _draftLength = "";
    private bool _editingLength;
    private readonly StackPanel _detailActions = new() { Spacing = 6 };
    private Border _leftPanel = null!, _rightPanel = null!;
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
        UseLayoutRounding = true;
        RenderOptions.SetBitmapInterpolationMode(_logo, BitmapInterpolationMode.HighQuality);
        FontSize = 16;
        // Poppins is used when bundled; this explicit fallback keeps offline startup usable.
        FontFamily = AssetLoader.Exists(new Uri("avares://MagasinOutil.Desktop/Assets/Poppins-Regular.ttf"))
            ? new FontFamily("avares://MagasinOutil.Desktop/Assets#Poppins") : Avalonia.Media.FontFamily.Default;
        var header = new Grid { ColumnDefinitions = new("Auto,*,Auto"), Margin = new(16, 8) };
        // The clear space is based on the original monogram/name separation (X).
        var logoSpace = new Border { Child = _logo, Padding = new(26, 14), Margin = new(0, 0, 12, 0) };
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
        _spindle = Action("", () => SelectPosition(ToolPosition.Spindle));
        _prepared = Action("", () => SelectPosition(ToolPosition.Prepared));
        _spindle.Margin = new(0, 0, 6, 0); _prepared.Margin = new(6, 0, 0, 0);
        Put(current, _spindle, 0, 0); Put(current, _prepared, 0, 1);
        Put(_shell, current, 1, 0);
        _body.Margin = new(16, 0, 16, 10);
        _leftPanel = Surface(_left); _leftPanel.Name = "MagazinePanel"; _leftPanel.Margin = new(0, 0, 6, 0);
        var detailLayout = new Grid { Name = "ToolLayout", RowDefinitions = new("*,Auto") };
        Put(detailLayout, _detail, 0, 0); Put(detailLayout, _detailActions, 1, 0);
        _rightPanel = Surface(detailLayout); _rightPanel.Name = "ToolPanel"; _rightPanel.Margin = new(6, 0, 0, 0);
        Put(_body, _leftPanel, 0, 0); Put(_body, _rightPanel, 0, 1);
        Put(_shell, _body, 2, 0);
        var footer = new Grid { ColumnDefinitions = new("*,Auto"), Margin = new(16, 0, 16, 10) };
        Put(footer, _notice, 0, 0); Put(footer, Label("Simulation · aucune liaison machine", 12), 0, 1);
        Put(_shell, footer, 3, 0);
        _left.Children.Add(Label("Magasin · 137 places physiques", 18));
        _left.Children.Add(_search); _left.Children.Add(_rackArea);
        _left.Children.Add(_rackTitle); _left.Children.Add(_slots); _left.Children.Add(_legend);

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
        var border = new Border { Padding = new(12), CornerRadius = new(8), Child = child };
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
    { ToolPosition.Spindle => "En broche", ToolPosition.Prepared => "Préparé", _ => "Dans le magasin" };
    private static string State(MagasinOutil.Core.Location l) => l.Forbidden ? "Interdit" : l.Blocked ? "Bloqué" : l.Tool is null ? "Vide" :
        l.Tool.Condition == ToolCondition.Defective ? "Défectueux" : l.Tool.Condition == ToolCondition.EndOfLife ? "Fin de vie" :
        l.Present ? "Présent" : PositionLabel(l.Tool.Position);
    private void Select(int location)
    {
        if (IsBusy) return;
        var item = _service.Read().Single(l => l.Number == location);
        _selected = location; _rack = item.Rack; _notice.Text = ""; Render();
    }
    private void Render()
    {
        var locations = _service.Read();
        foreach (var pair in new[] { (_spindle, ToolPosition.Spindle, "En broche"), (_prepared, ToolPosition.Prepared, "Préparé") })
        {
            var item = locations.FirstOrDefault(l => l.Tool?.Position == pair.Item2);
            pair.Item1.Content = pair.Item3 + (item?.Tool is {} t ? $" · T{t.Id} · consulter" : " · Aucun outil");
            pair.Item1.IsEnabled = !IsBusy && item is not null;
            pair.Item1.HorizontalAlignment = HorizontalAlignment.Stretch;
            pair.Item1.HorizontalContentAlignment = HorizontalAlignment.Left;
        }
        _search.IsEnabled = !IsBusy;
        _leftPanel.IsVisible = _draft is null;
        Grid.SetColumn(_rightPanel, _draft is null ? 1 : 0);
        Grid.SetColumnSpan(_rightPanel, _draft is null ? 1 : 2);
        _rightPanel.Margin = _draft is null ? new Thickness(6, 0, 0, 0) : new Thickness(0);
        RenderLocations(); RenderDetail();
    }
    private void RenderLocations()
    {
        var locations = _service.Read(); _rackArea.Children.Clear(); _slots.Children.Clear();
        _legend.Children.Clear();
        foreach (var entry in new[] { ("Present", "● Présent"), ("Outside", "↗ Hors magasin"),
            ("Defective", "! Défectueux"), ("EndOfLife", "! Fin de vie"), ("Neutral", "× Indisponible / — Vide") })
        {
            var palette = StatusPalette(entry.Item1);
            _legend.Children.Add(new Border { Background = palette.Background, CornerRadius = new(4),
                Padding = new(6, 4), Margin = new(0, 0, 5, 5), Child = new TextBlock
                { Text = entry.Item2, FontSize = 12, Foreground = palette.Foreground } });
        }
        var query = (_search.Text ?? "").Trim();
        _slots.IsVisible = _rackTitle.IsVisible = _legend.IsVisible = query.Length == 0;
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
                button.IsEnabled = !IsBusy; button.Height = 64; button.HorizontalAlignment = HorizontalAlignment.Stretch; _rackArea.Children.Add(button);
            }
            var pages = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var prev = Action("Précédent", () => { _page--; RenderLocations(); }); prev.IsEnabled = _page > 0 && !IsBusy;
            var next = Action("Suivant", () => { _page++; RenderLocations(); }); next.IsEnabled = (_page + 1) * 4 < found.Length && !IsBusy;
            pages.Children.Add(prev); pages.Children.Add(next); _rackArea.Children.Add(pages);
        }
        else
        {
            foreach (var group in new[] { new[] {5,4,3,2,1}, new[] {6,7,8} })
            {
                _rackArea.Children.Add(Label(group[0] == 5 ? "Partie supérieure · places" : "Partie inférieure · places", 13));
                var row = new WrapPanel();
                foreach (var rack in group)
                {
                    var range = locations.Where(l => l.Rack == rack).ToArray();
                    var button = Action($"Rack {rack}", () => Select(locations.First(l => l.Rack == rack).Number));
                    var rackText = new StackPanel { Spacing = 2 };
                    rackText.Children.Add(Label($"Rack {rack}", 15));
                    rackText.Children.Add(new TextBlock { Text = $"{range.First().Number}–{range.Last().Number}", FontSize = 12, TextWrapping = TextWrapping.NoWrap });
                    button.Content = rackText;
                    button.Width = 88; button.Height = 52; button.Padding = new(6); button.Margin = new(0, 0, 5, 0); button.IsEnabled = !IsBusy;
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
            button.Content = new TextBlock { Text = $"{l.Number} {mark}", FontSize = 15, TextWrapping = TextWrapping.NoWrap };
            button.Height = 44; button.Padding = new(3); button.Margin = new(0, 0, 4, 4);
            button.HorizontalAlignment = HorizontalAlignment.Stretch; button.IsEnabled = !IsBusy;
            Avalonia.Automation.AutomationProperties.SetName(button, $"Place {l.Number}, {State(l)}");
            var colors = StatusColors(l);
            button.Background = colors.Background; button.Foreground = colors.Foreground;
            button.BorderThickness = new(3);
            button.BorderBrush = _selected == l.Number ? Ink : Brushes.Transparent;
            _slots.Children.Add(button);
        }
    }
    private void RenderDetail()
    {
        _detail.Children.Clear(); _detailActions.Children.Clear(); _wearInput = null;
        var location = _service.Read().Single(l => l.Number == _selected);
        var heading = new Grid { ColumnDefinitions = new("*,Auto") };
        Put(heading, Label($"PLACE {location.Number} · RACK {location.Rack}", 13), 0, 0);
        var colors = StatusColors(location);
        Put(heading, new Border { Background = colors.Background, CornerRadius = new(6), Padding = new(8, 4),
            Child = new TextBlock { Text = State(location), Foreground = colors.Foreground, FontSize = 12 } }, 0, 1);
        _detail.Children.Add(heading);
        if (location.Tool is not { } tool) { _detail.Children.Add(Label("Aucun outil modifiable à cet emplacement.")); return; }
        _detail.Children.Add(Label($"Outil T{tool.Id}", 22));
        if (_transfer is not null)
        {
            _detail.Children.Add(Label(_transfer.Destination == ToolPosition.Prepared ? "Préparer cet outil ?" : "Charger cet outil en broche ?", 19));
            _detail.Children.Add(Label(tool.Name));
            _detail.Children.Add(Label("Simulation uniquement · aucun mouvement machine.", 13));
            if (_transfer.ExpectedOccupantId is int occupant)
                _detail.Children.Add(Label($"T{occupant} sera replacé à sa place fixe dans le simulateur.", 14));
            _detail.Children.Add(Action("Annuler", () => { _transfer = null; Render(); }));
            _detail.Children.Add(Action("Confirmer la simulation", () =>
            {
                if (_transfer is null) return;
                var result = _service.Transfer(_transfer); _transfer = null;
                _notice.Text = result.Message; Render();
            }));
            return;
        }
        if (_draft is not null)
        {
            if (_review)
            {
                _detail.Children.Add(Label("Vérifier la modification", 18));
                _detail.Children.Add(Label($"Nom : {tool.Name} → {_draft.Name}"));
                _detail.Children.Add(Label($"Longueur : {Number(tool.Length)} → {Number(_draft.Length ?? tool.Length)} mm"));
                _detail.Children.Add(Label($"Usure : {Number(tool.Wear)} → {Number(_draft.Wear)} mm"));
                _detail.Children.Add(Label("Application simulée à cet outil uniquement.", 13));
                _detail.Children.Add(Action("Retour", () => { _review = false; RenderDetail(); }));
                _detail.Children.Add(Action("Confirmer la simulation", Apply));
                return;
            }
            _detail.Children.Add(Label("Modifier le nom, la longueur et l’usure · simulation", 18));
            var editor = new Grid { ColumnDefinitions = new("*,*") };
            var fields = new StackPanel { Spacing = 8, Margin = new(0, 0, 20, 0) };
            fields.Children.Add(Label("Nom de l’outil", 13));
            var name = new TextBox { Text = _draftName, MaxLength = 30, MinHeight = 44 };
            name.TextChanged += (_, _) => _draftName = name.Text ?? ""; fields.Children.Add(name);
            fields.Children.Add(Label("Longueur · mm (simulation)", 13));
            var length = new TextBox { Text = _draftLength, MinHeight = 44 };
            length.TextChanged += (_, _) => _draftLength = length.Text ?? "";
            length.GotFocus += (_, _) => { _wearInput = length; _editingLength = true; };
            fields.Children.Add(length);
            fields.Children.Add(Label("Usure longueur · mm (simulation)", 13));
            var wear = new TextBox { Text = _draftWear, MinHeight = 44 };
            wear.TextChanged += (_, _) => _draftWear = wear.Text ?? "";
            wear.GotFocus += (_, _) => { _wearInput = wear; _editingLength = false; };
            fields.Children.Add(wear);
            _wearInput = _editingLength ? length : wear;
            Put(editor, fields, 0, 0);
            var numeric = new StackPanel { Spacing = 10, Margin = new(20, 0, 0, 0) };
            numeric.Children.Add(Label("Pavé numérique", 17));
            numeric.Children.Add(Label("Sélectionner Longueur ou Usure pour saisir la valeur.", 13));
            var pad = new Grid { ColumnDefinitions = new("*,*,*,*"), RowDefinitions = new("Auto,Auto,Auto,Auto") };
            string[] keys = ["7","8","9","⌫","4","5","6","±","1","2","3","Effacer","0",","];
            for (var i = 0; i < keys.Length; i++)
            { var key = keys[i]; var button = Action(key, () => Keypad(key)); button.Margin = new(2); button.HorizontalAlignment = HorizontalAlignment.Stretch; Put(pad, button, i / 4, i % 4); }
            numeric.Children.Add(pad); Put(editor, numeric, 0, 1); _detail.Children.Add(editor);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            actions.Children.Add(Action("Annuler", () => { _draft = null; _notice.Text = "Modification annulée."; Render(); }));
            actions.Children.Add(Action("Vérifier", Review)); _detailActions.Children.Add(actions); return;
        }
        var toolName = new TextBlock { Text = tool.Name, FontSize = 18, TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis };
        ToolTip.SetTip(toolName, tool.Name); _detail.Children.Add(toolName);
        var summary = new Grid { ColumnDefinitions = new("*,*"), Margin = new(0, 4, 0, 6) };
        Put(summary, ValueTile("Place affectée", location.Number.ToString()), 0, 0);
        Put(summary, ValueTile("Position actuelle", PositionLabel(tool.Position)), 0, 1);
        _detail.Children.Add(summary);
        var tabs = new Grid { ColumnDefinitions = new("*,*") };
        var dataTab = Action("Vue d’ensemble", () => { _offsets = false; RenderDetail(); });
        var offsetTab = Action("Correcteurs", () => { _offsets = true; RenderDetail(); });
        foreach (var tab in new[] { dataTab, offsetTab }) tab.HorizontalAlignment = HorizontalAlignment.Stretch;
        var activeTab = _offsets ? offsetTab : dataTab; activeTab.Background = Selected; activeTab.Foreground = Ink;
        Put(tabs, dataTab, 0, 0); Put(tabs, offsetTab, 0, 1); _detail.Children.Add(tabs);
        _detail.Children.Add(ValueRow("Longueur", Number(tool.Length) + " mm"));
        if (_offsets)
        {
            _detail.Children.Add(ValueRow("Usure longueur", Number(tool.Wear) + " mm"));
            _detail.Children.Add(Label("Correcteur de fraisage fictif n° 1", 12));
        }
        else _detail.Children.Add(ValueRow("Présence à la place", location.Present ? "Oui" : "Non"));
        _detail.Children.Add(Action("Modifier les données", () =>
        {
            _draft = new(location.Number, tool.Id, tool.Revision, tool.Name, tool.Wear, tool.Length);
            _draftName = tool.Name; _draftWear = Number(tool.Wear); _draftLength = Number(tool.Length); _review = false; _notice.Text = "Brouillon · aucune écriture machine"; Render();
        }));
        _detailActions.Children.Add(new Separator { Margin = new(0, 2) });
        _detailActions.Children.Add(Label("Actions sur cet outil · simulation", 14));
        var transferActions = new Grid { ColumnDefinitions = new("*,*") };
        var prepare = Action("Préparer", () => BeginTransfer(ToolPosition.Prepared));
        var load = Action("Charger en broche", () => BeginTransfer(ToolPosition.Spindle));
        prepare.IsEnabled = tool.Condition == ToolCondition.Available && tool.Position == ToolPosition.Magazine;
        load.IsEnabled = tool.Condition == ToolCondition.Available && tool.Position != ToolPosition.Spindle;
        prepare.HorizontalAlignment = load.HorizontalAlignment = HorizontalAlignment.Stretch;
        prepare.Margin = new(0, 0, 4, 0); load.Margin = new(4, 0, 0, 0);
        Put(transferActions, prepare, 0, 0); Put(transferActions, load, 0, 1); _detailActions.Children.Add(transferActions);
        if (tool.Condition != ToolCondition.Available)
            _detailActions.Children.Add(Label("Actions indisponibles : outil défectueux ou en fin de vie.", 13));
        else if (tool.Position != ToolPosition.Magazine)
            _detailActions.Children.Add(Label(tool.Position == ToolPosition.Spindle ? "Cet outil est déjà en broche." : "Cet outil est déjà préparé.", 13));
    }
    private Control ValueTile(string title, string value)
    {
        var panel = new StackPanel { Spacing = 4, Margin = new(0, 0, 8, 0) };
        panel.Children.Add(Label(title, 12)); panel.Children.Add(Label(value, 17)); return panel;
    }
    private static Control ValueRow(string title, string value)
    {
        var grid = new Grid { ColumnDefinitions = new("*,Auto"), Margin = new(0, 3) };
        Put(grid, Label(title, 14), 0, 0); Put(grid, Label(value), 0, 1); return grid;
    }
    private (IBrush Background, IBrush Foreground) StatusColors(MagasinOutil.Core.Location l)
        => StatusPalette(l.Forbidden || l.Blocked || l.Tool is null ? "Neutral" :
            l.Tool.Condition == ToolCondition.Defective ? "Defective" :
            l.Tool.Condition == ToolCondition.EndOfLife ? "EndOfLife" : l.Present ? "Present" : "Outside");
    private (IBrush Background, IBrush Foreground) StatusPalette(string state)
    {
        if (state == "Neutral")
            return (Brush(_dark ? "#343E50" : "#EAECF0"), Brush(_dark ? "#DCE1EB" : "#485366"));
        if (state == "Defective")
            return (Brush(_dark ? "#582C36" : "#FCE8EB"), Brush(_dark ? "#FFB6C1" : "#972B42"));
        if (state == "EndOfLife")
            return (Brush(_dark ? "#4D3D22" : "#FFF1D7"), Brush(_dark ? "#FFDA96" : "#78500C"));
        if (state == "Outside")
            return (Brush(_dark ? "#273F67" : "#E5EDFF"), Brush(_dark ? "#B9D1FF" : "#264D91"));
        return (Brush(_dark ? "#23443E" : "#E5F4ED"), Brush(_dark ? "#A2E4C8" : "#205F49"));
    }
    private void SelectPosition(ToolPosition position)
    {
        var item = _service.Read().FirstOrDefault(l => l.Tool?.Position == position);
        if (item is not null) Select(item.Number);
    }
    private void BeginTransfer(ToolPosition destination)
    {
        if (IsBusy) return;
        var snapshot = _service.Read(); var location = snapshot.Single(l => l.Number == _selected);
        if (location.Tool is not {} tool) return;
        var occupant = snapshot.FirstOrDefault(l => l.Tool?.Position == destination)?.Tool;
        _transfer = new(location.Number, tool.Id, tool.Revision, destination, occupant?.Id, occupant?.Revision);
        _notice.Text = ""; Render();
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
                CultureInfo.InvariantCulture, out var wear) || decimal.Round(wear, 3) != wear ||
            !decimal.TryParse(_draftLength.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var length) || length < 0 || decimal.Round(length, 3) != length)
        { _notice.Text = "Vérifier le nom, la longueur positive ou nulle et l’usure (trois décimales maximum)."; return; }
        _draft = _draft with { Name = _draftName.Trim(), Wear = wear, Length = length }; _review = true; _notice.Text = ""; RenderDetail();
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
