using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MagasinOutil.Core;

namespace MagasinOutil.Desktop;

public sealed class LoginWindow : Window
{
    private readonly Func<string, string, Task<ProductSignInResult>> _signIn;
    private readonly Action<ProductSessionView> _authenticated;
    private readonly TextBox _userName = new() { MinHeight = 44, PlaceholderText = "Utilisateur" };
    private readonly TextBox _password = new() { MinHeight = 44, PlaceholderText = "Mot de passe", PasswordChar = '●' };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button _connect = new() { Content = "Se connecter", MinHeight = 48, HorizontalAlignment = HorizontalAlignment.Stretch };

    public LoginWindow(
        Func<string, string, Task<ProductSignInResult>> signIn,
        Action<ProductSessionView> authenticated)
    {
        _signIn = signIn;
        _authenticated = authenticated;
        Title = "Gestion des outils 8xx — Connexion";
        Width = 680;
        Height = 560;
        MinWidth = 680;
        MinHeight = 560;
        CanResize = false;
        Background = new SolidColorBrush(Color.Parse("#F1F2F5"));
        FontSize = 16;

        var root = new Grid { ColumnDefinitions = new("*,520,*"), RowDefinitions = new("*,Auto,*") };
        var card = new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(34),
        };
        Grid.SetColumn(card, 1);
        Grid.SetRow(card, 1);
        root.Children.Add(card);

        var panel = new StackPanel { Spacing = 14 };
        card.Child = panel;
        panel.Children.Add(new TextBlock
        {
            Text = "Gestion des outils 8xx",
            FontSize = 27,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#202945")),
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Plateforme Multi-Technologie · Session locale gouvernée",
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.Parse("#526078")),
        });

        panel.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.Parse("#FFF1D7")),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 7),
            Child = new TextBlock
            {
                Text = "PROFIL DÉMONSTRATION · comptes locaux non destinés à la production",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#78500C")),
                TextWrapping = TextWrapping.Wrap,
            },
        });

        panel.Children.Add(Label("Utilisateur"));
        panel.Children.Add(_userName);
        panel.Children.Add(Label("Mot de passe"));
        panel.Children.Add(_password);
        panel.Children.Add(new TextBlock { Text = "Profils rapides", FontSize = 13 });

        var profiles = new WrapPanel();
        profiles.Children.Add(Profile("Consultation", "consultation"));
        profiles.Children.Add(Profile("Opérateur", "operateur"));
        profiles.Children.Add(Profile("Régleur", "regleur"));
        profiles.Children.Add(Profile("Administrateur", "admin"));
        panel.Children.Add(profiles);

        _connect.Click += async (_, _) => await SignInAsync();
        _password.KeyDown += async (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                e.Handled = true;
                await SignInAsync();
            }
        };
        panel.Children.Add(_connect);
        panel.Children.Add(_status);
        Content = root;
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
                _status.Text = "Session établie.";
                _authenticated(result.Session);
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
            MinHeight = 36,
            Padding = new Thickness(10, 4),
            Margin = new Thickness(0, 0, 6, 6),
        };
        button.Click += (_, _) =>
        {
            _userName.Text = userName;
            _password.Focus();
        };
        return button;
    }

    private static TextBlock Label(string text) => new() { Text = text, FontSize = 13 };
}
