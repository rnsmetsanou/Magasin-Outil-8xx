using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using MagasinOutil.Core;

namespace MagasinOutil.Desktop;

public sealed class LoginWindow : Window
{
    private readonly Func<string, string, Task<ProductSignInResult>> _signIn;
    private readonly Func<ProductSessionView, Task> _authenticated;
    private readonly Action<bool>? _themeChanged;
    private readonly TextBox _userName = new() { MinHeight = 46, PlaceholderText = "Nom d'utilisateur" };
    private readonly TextBox _password = new() { MinHeight = 46, PlaceholderText = "Mot de passe", PasswordChar = '●' };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, FontSize = 13 };
    private readonly Button _connect = new()
    {
        Content = "Se connecter",
        MinHeight = 50,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        FontWeight = FontWeight.SemiBold,
    };
    private readonly Grid _root = new() { ColumnDefinitions = new("330,*") };
    private readonly Border _formSurface = new() { CornerRadius = new CornerRadius(14), Padding = new Thickness(34, 30) };
    private readonly TextBlock _formTitle = new() { Text = "Connexion", FontSize = 28, FontWeight = FontWeight.SemiBold };
    private readonly TextBlock _formSubtitle = new()
    {
        Text = "Accédez au Magasin d'outils avec une session locale gouvernée.",
        FontSize = 13,
        TextWrapping = TextWrapping.Wrap,
    };
    private readonly Border _demoBanner = new() { CornerRadius = new CornerRadius(7), Padding = new Thickness(11, 8) };
    private readonly TextBlock _demoText = new()
    {
        Text = "PROFIL DÉMONSTRATION · comptes locaux non destinés à la production",
        FontSize = 11.5,
        TextWrapping = TextWrapping.Wrap,
    };
    private readonly Button _lightTheme = new() { Content = "☀  Clair", MinHeight = 36, Padding = new Thickness(11, 5) };
    private readonly Button _darkTheme = new() { Content = "☾  Sombre", MinHeight = 36, Padding = new Thickness(11, 5) };
    private readonly Bitmap _whiteLogo = new(AssetLoader.Open(new Uri("avares://MagasinOutil.Desktop/Assets/wm-logo-white.png")));

    public LoginWindow(
        Func<string, string, Task<ProductSignInResult>> signIn,
        Func<ProductSessionView, Task> authenticated)
        : this(signIn, authenticated, false, null)
    {
    }

    public LoginWindow(
        Func<string, string, Task<ProductSignInResult>> signIn,
        Func<ProductSessionView, Task> authenticated,
        bool initialDark,
        Action<bool>? themeChanged)
    {
        _signIn = signIn;
        _authenticated = authenticated;
        _themeChanged = themeChanged;

        Title = "Gestion des outils 8xx — Connexion";
        Width = 900;
        Height = 610;
        MinWidth = 900;
        MinHeight = 610;
        CanResize = false;
        UseLayoutRounding = true;
        FontSize = 15;

        BuildBrandPanel();
        BuildFormPanel();
        Content = _root;

        _connect.Click += async (_, _) => await SignInAsync();
        _password.KeyDown += async (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                e.Handled = true;
                await SignInAsync();
            }
        };
        _lightTheme.Click += (_, _) => ApplyTheme(false, true);
        _darkTheme.Click += (_, _) => ApplyTheme(true, true);
        Closed += (_, _) => _whiteLogo.Dispose();

        ApplyTheme(initialDark, false);
    }

    private void BuildBrandPanel()
    {
        var brand = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#10213C")),
            Padding = new Thickness(34),
        };
        Grid.SetColumn(brand, 0);
        _root.Children.Add(brand);

        var panel = new Grid { RowDefinitions = new("Auto,*,Auto") };
        brand.Child = panel;

        var identity = new StackPanel { Spacing = 16 };
        identity.Children.Add(new Image
        {
            Source = _whiteLogo,
            Width = 185,
            Height = 74,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Left,
        });
        identity.Children.Add(new TextBlock
        {
            Text = "Gestion des outils 8xx",
            FontSize = 27,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
        });
        identity.Children.Add(new TextBlock
        {
            Text = "Une HMI métier portée par le socle Plateforme Multi-Technologie.",
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.Parse("#C7D1E2")),
            TextWrapping = TextWrapping.Wrap,
        });
        Grid.SetRow(identity, 0);
        panel.Children.Add(identity);

        var capabilities = new StackPanel
        {
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
        };
        capabilities.Children.Add(BrandFeature("●", "Session gouvernée", "Identité locale et droits effectifs"));
        capabilities.Children.Add(BrandFeature("●", "Licence hors ligne", "Capacités signées et liées à l'installation"));
        capabilities.Children.Add(BrandFeature("●", "Admission durable", "Intentions et opérations traçables"));
        Grid.SetRow(capabilities, 1);
        panel.Children.Add(capabilities);

        var footer = new TextBlock
        {
            Text = ".NET 10 · Avalonia · Démonstration locale",
            FontSize = 11.5,
            Foreground = new SolidColorBrush(Color.Parse("#93A5C2")),
        };
        Grid.SetRow(footer, 2);
        panel.Children.Add(footer);
    }

    private void BuildFormPanel()
    {
        var formHost = new Grid
        {
            RowDefinitions = new("*,Auto,*"),
            Margin = new Thickness(42, 24),
        };
        Grid.SetColumn(formHost, 1);
        _root.Children.Add(formHost);

        Grid.SetRow(_formSurface, 1);
        formHost.Children.Add(_formSurface);

        var panel = new StackPanel { Spacing = 13 };
        _formSurface.Child = panel;

        var heading = new Grid { ColumnDefinitions = new("*,Auto") };
        var titleGroup = new StackPanel { Spacing = 4 };
        titleGroup.Children.Add(_formTitle);
        titleGroup.Children.Add(_formSubtitle);
        Grid.SetColumn(titleGroup, 0);
        heading.Children.Add(titleGroup);

        var themes = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Top,
        };
        themes.Children.Add(_lightTheme);
        themes.Children.Add(_darkTheme);
        Grid.SetColumn(themes, 1);
        heading.Children.Add(themes);
        panel.Children.Add(heading);

        _demoBanner.Child = _demoText;
        panel.Children.Add(_demoBanner);

        panel.Children.Add(Label("Utilisateur"));
        panel.Children.Add(_userName);
        panel.Children.Add(Label("Mot de passe"));
        panel.Children.Add(_password);

        var quickHeader = new Grid { ColumnDefinitions = new("*,Auto") };
        quickHeader.Children.Add(new TextBlock
        {
            Text = "Profils de démonstration",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        });
        var hint = new TextBlock
        {
            Text = "Sélection rapide du compte",
            FontSize = 11.5,
            Opacity = 0.65,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(hint, 1);
        quickHeader.Children.Add(hint);
        panel.Children.Add(quickHeader);

        var profiles = new UniformGrid { Columns = 2, Rows = 2 };
        profiles.Children.Add(Profile("Consultation", "consultation"));
        profiles.Children.Add(Profile("Opérateur", "operateur"));
        profiles.Children.Add(Profile("Régleur outils", "regleur"));
        profiles.Children.Add(Profile("Administrateur", "admin"));
        panel.Children.Add(profiles);

        panel.Children.Add(_connect);
        panel.Children.Add(_status);
    }

    private async Task SignInAsync()
    {
        if (!_connect.IsEnabled) return;
        _connect.IsEnabled = false;
        _status.Text = "Authentification…";
        try
        {
            var result = await _signIn(_userName.Text ?? string.Empty, _password.Text ?? string.Empty);
            if (result.IsAuthenticated && result.Session is not null)
            {
                _status.Text = "Session établie · lecture du Core…";
                await _authenticated(result.Session);
                return;
            }

            _status.Text = result.Reason;
        }
        catch (Exception)
        {
            _status.Text = "CoreHost indisponible. Vérifier que le profil de démonstration est démarré.";
        }
        finally
        {
            _connect.IsEnabled = true;
        }
    }

    private Button Profile(string label, string userName)
    {
        var button = new Button
        {
            Content = label,
            MinHeight = 42,
            Padding = new Thickness(10, 6),
            Margin = new Thickness(0, 0, 7, 7),
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        button.Click += (_, _) =>
        {
            _userName.Text = userName;
            _password.Focus();
        };
        return button;
    }

    private void ApplyTheme(bool dark, bool notify)
    {
        RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        _root.Background = new SolidColorBrush(Color.Parse(dark ? "#081523" : "#EEF1F5"));
        _formSurface.Background = new SolidColorBrush(Color.Parse(dark ? "#13263A" : "#FFFFFF"));
        Foreground = new SolidColorBrush(Color.Parse(dark ? "#F3F6FA" : "#202945"));
        _formTitle.Foreground = Foreground;
        _formSubtitle.Foreground = new SolidColorBrush(Color.Parse(dark ? "#B7C4D6" : "#526078"));
        _demoBanner.Background = new SolidColorBrush(Color.Parse(dark ? "#473919" : "#FFF1D7"));
        _demoText.Foreground = new SolidColorBrush(Color.Parse(dark ? "#FFD982" : "#78500C"));
        _status.Foreground = Foreground;
        _lightTheme.Opacity = dark ? 0.68 : 1.0;
        _darkTheme.Opacity = dark ? 1.0 : 0.68;
        if (notify) _themeChanged?.Invoke(dark);
    }

    private static Control BrandFeature(string marker, string title, string subtitle)
    {
        var grid = new Grid { ColumnDefinitions = new("26,*") };
        var mark = new TextBlock
        {
            Text = marker,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.Parse("#66D7B0")),
            VerticalAlignment = VerticalAlignment.Top,
        };
        grid.Children.Add(mark);
        var text = new StackPanel { Spacing = 2 };
        text.Children.Add(new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White });
        text.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 11.5,
            Foreground = new SolidColorBrush(Color.Parse("#AEBBD0")),
            TextWrapping = TextWrapping.Wrap,
        });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        return grid;
    }

    private static TextBlock Label(string text) => new()
    {
        Text = text,
        FontSize = 12.5,
        FontWeight = FontWeight.SemiBold,
    };
}
