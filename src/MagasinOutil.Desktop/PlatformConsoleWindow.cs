using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MagasinOutil.Core;

namespace MagasinOutil.Desktop;

internal sealed class PlatformConsoleWindow : Window
{
    private readonly IProductAdministrationReadService _service;
    private readonly ProductSessionRequest _request;
    private readonly Grid _content = new();

    public PlatformConsoleWindow(
        IProductAdministrationReadService service,
        ProductSessionView session,
        string clientId)
    {
        _service = service;
        _request = new ProductSessionRequest(ProductSessionContract.Version, session.SessionReference, clientId);
        Title = "Plateforme · Sécurité, licence et audit";
        Width = 1080;
        Height = 760;
        MinWidth = 900;
        MinHeight = 620;
        CanResize = true;

        var root = new Grid { RowDefinitions = new("Auto,*") };
        var header = new Grid
        {
            ColumnDefinitions = new("*,Auto"),
            Margin = new Thickness(22, 18, 22, 12),
        };
        var heading = new StackPanel { Spacing = 3 };
        heading.Children.Add(new TextBlock { Text = "Socle Plateforme", FontSize = 24, FontWeight = FontWeight.SemiBold });
        heading.Children.Add(new TextBlock
        {
            Text = "Identité · session · droits · licence · admission durable",
            FontSize = 13,
            Opacity = 0.72,
        });
        Grid.SetColumn(heading, 0);
        header.Children.Add(heading);

        var close = new Button { Content = "Fermer", MinHeight = 40, Padding = new Thickness(16, 8) };
        close.Click += (_, _) => Close();
        Grid.SetColumn(close, 1);
        header.Children.Add(close);
        root.Children.Add(header);

        Grid.SetRow(_content, 1);
        _content.Children.Add(new TextBlock
        {
            Text = "Chargement des données CoreHost…",
            Margin = new Thickness(22),
            FontSize = 16,
        });
        root.Children.Add(_content);
        Content = root;

        Opened += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        ProductAdministrationReadResult result;
        try
        {
            result = await _service.ReadAdministrationAsync(_request);
        }
        catch
        {
            result = new ProductAdministrationReadResult(
                ProductAdministrationReadStatus.Unavailable,
                null,
                "CoreHost indisponible.");
        }

        _content.Children.Clear();
        if (!result.IsSuccess || result.Snapshot is not { } snapshot)
        {
            _content.Children.Add(new TextBlock
            {
                Text = result.Reason,
                Margin = new Thickness(22),
                FontSize = 16,
            });
            return;
        }

        var tabs = new TabControl
        {
            Margin = new Thickness(18, 0, 18, 18),
            ItemsSource = new[]
            {
                new TabItem { Header = "Session & droits", Content = Scroll(Session(snapshot)) },
                new TabItem { Header = "Licence", Content = Scroll(License(snapshot.License)) },
                new TabItem { Header = "Utilisateurs", Content = Scroll(Users(snapshot)) },
                new TabItem { Header = "Audit durable", Content = Scroll(Audit(snapshot)) },
            },
        };
        _content.Children.Add(tabs);
    }

    private static Control Session(ProductAdministrationSnapshot snapshot)
    {
        var session = snapshot.Session;
        var panel = Section();
        panel.Children.Add(SectionTitle("Session CoreHost"));
        panel.Children.Add(StatusLine("Utilisateur", $"{session.DisplayName} ({session.UserName})"));
        panel.Children.Add(StatusLine("Sujet", session.SubjectId));
        panel.Children.Add(StatusLine("Émise", Format(session.IssuedAt)));
        panel.Children.Add(StatusLine("Expiration absolue", Format(session.AbsoluteExpiresAt)));
        panel.Children.Add(StatusLine("Révision de politique", session.PolicyRevision.ToString(CultureInfo.InvariantCulture)));
        panel.Children.Add(Subtitle("Permissions effectives"));
        panel.Children.Add(Chips(session.Permissions));
        panel.Children.Add(new TextBlock
        {
            Text = "Les permissions ci-dessus sont fournies par le CoreHost. L'interface n'est pas l'autorité d'autorisation.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Opacity = 0.72,
            Margin = new Thickness(0, 12, 0, 0),
        });
        return panel;
    }

    private static Control License(ProductLicenseView license)
    {
        var panel = Section();
        panel.Children.Add(SectionTitle("Licence produit"));
        panel.Children.Add(StatusBadge(
            license.Status == "Valid" ? "● LICENCE ACTIVE" : "○ " + license.Status.ToUpperInvariant(),
            license.Status == "Valid"));
        panel.Children.Add(StatusLine("Installation", Empty(license.InstallationId)));
        panel.Children.Add(StatusLine("Licence", Empty(license.LicenseId)));
        panel.Children.Add(StatusLine("Renouvellement", license.RenewalVersion?.ToString(CultureInfo.InvariantCulture) ?? "—"));
        panel.Children.Add(StatusLine("Valide depuis", license.ValidFromUtc is { } validFrom ? Format(validFrom) : "—"));
        panel.Children.Add(StatusLine("Expiration", license.ExpiresAtUtc is { } expires ? Format(expires) : "Sans expiration déclarée"));
        panel.Children.Add(Subtitle("Capacités signées"));
        panel.Children.Add(Chips(license.Capabilities));
        panel.Children.Add(new TextBlock
        {
            Text = license.Message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0),
            Opacity = 0.75,
        });
        return panel;
    }

    private static Control Users(ProductAdministrationSnapshot snapshot)
    {
        var panel = Section();
        panel.Children.Add(SectionTitle("Utilisateurs & rôles"));
        if (!snapshot.CanViewUsers)
        {
            panel.Children.Add(Restricted("Accès réservé à identity.manage ou roles.manage."));
            panel.Children.Add(new TextBlock
            {
                Text = "Votre session reste visible dans l'onglet Session & droits.",
                Margin = new Thickness(0, 10, 0, 0),
                Opacity = 0.72,
            });
            return panel;
        }

        foreach (var user in snapshot.Users)
        {
            var card = new StackPanel { Spacing = 6 };
            card.Children.Add(new TextBlock
            {
                Text = $"{user.DisplayName} · {user.Role}",
                FontSize = 17,
                FontWeight = FontWeight.SemiBold,
            });
            card.Children.Add(new TextBlock { Text = $"Compte : {user.UserName} · {(user.IsEnabled ? "Actif" : "Désactivé")}", FontSize = 13 });
            card.Children.Add(Chips(user.Permissions));
            panel.Children.Add(Card(card));
        }

        panel.Children.Add(new TextBlock
        {
            Text = "Important : le rôle Administrateur ne reçoit pas implicitement les commandes machine tool.prepare / tool.load.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0),
            FontWeight = FontWeight.SemiBold,
        });
        return panel;
    }

    private static Control Audit(ProductAdministrationSnapshot snapshot)
    {
        var panel = Section();
        panel.Children.Add(SectionTitle(snapshot.AuditScope));
        if (!snapshot.CanViewAudit)
        {
            panel.Children.Add(Restricted("Accès réservé à audit.read."));
            return panel;
        }

        panel.Children.Add(new TextBlock
        {
            Text = "Cette vue montre les admissions persistées et qualifiées par D3. Elle ne prétend pas encore être le journal de sécurité complet T2.4-C.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Opacity = 0.72,
            Margin = new Thickness(0, 0, 0, 12),
        });

        if (snapshot.AuditEntries.Count == 0)
        {
            panel.Children.Add(new TextBlock { Text = "Aucune admission durable enregistrée pour le moment." });
            return panel;
        }

        foreach (var entry in snapshot.AuditEntries)
        {
            var card = new StackPanel { Spacing = 4 };
            card.Children.Add(new TextBlock
            {
                Text = $"{entry.Action} · {entry.Decision}",
                FontSize = 16,
                FontWeight = FontWeight.SemiBold,
            });
            card.Children.Add(new TextBlock { Text = $"{Format(entry.RecordedAtUtc)} · {entry.SubjectId}" });
            card.Children.Add(new TextBlock { Text = $"Intent {Short(entry.IntentId)} · Op {Short(entry.OperationId)} · Policy {entry.PolicyRevision}", FontSize = 12, Opacity = 0.75 });
            panel.Children.Add(Card(card));
        }
        return panel;
    }

    private static ScrollViewer Scroll(Control content) => new()
    {
        Content = content,
        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
    };

    private static StackPanel Section() => new() { Spacing = 10, Margin = new Thickness(18) };
    private static TextBlock SectionTitle(string text) => new() { Text = text, FontSize = 22, FontWeight = FontWeight.SemiBold };
    private static TextBlock Subtitle(string text) => new() { Text = text, FontSize = 15, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 10, 0, 0) };

    private static Control StatusLine(string title, string value)
    {
        var grid = new Grid { ColumnDefinitions = new("190,*"), Margin = new Thickness(0, 2) };
        var left = new TextBlock { Text = title, FontSize = 13, Opacity = 0.70 };
        var right = new TextBlock { Text = value, FontSize = 14, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);
        return grid;
    }

    private static Control Chips(IEnumerable<string> values)
    {
        var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var value in values.OrderBy(static value => value, StringComparer.Ordinal))
        {
            wrap.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.Parse("#E8EDF7")),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(8, 4),
                Margin = new Thickness(0, 0, 6, 6),
                Child = new TextBlock { Text = value, Foreground = new SolidColorBrush(Color.Parse("#20304F")), FontSize = 12 },
            });
        }
        if (wrap.Children.Count == 0)
            wrap.Children.Add(new TextBlock { Text = "Aucune", Opacity = 0.65 });
        return wrap;
    }

    private static Border StatusBadge(string text, bool positive) => new()
    {
        Background = new SolidColorBrush(Color.Parse(positive ? "#DFF4E8" : "#FCE8EB")),
        CornerRadius = new CornerRadius(6),
        Padding = new Thickness(10, 7),
        HorizontalAlignment = HorizontalAlignment.Left,
        Child = new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.Parse(positive ? "#205F49" : "#972B42")),
            FontWeight = FontWeight.SemiBold,
        },
    };

    private static Border Restricted(string message) => new()
    {
        Background = new SolidColorBrush(Color.Parse("#FFF1D7")),
        CornerRadius = new CornerRadius(6),
        Padding = new Thickness(12),
        Child = new TextBlock { Text = "🔒 " + message, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.Parse("#78500C")) },
    };

    private static Border Card(Control child) => new()
    {
        BorderBrush = new SolidColorBrush(Color.Parse("#D8DEE9")),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(7),
        Padding = new Thickness(12),
        Margin = new Thickness(0, 4),
        Child = child,
    };

    private static string Empty(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
    private static string Format(DateTimeOffset value) => value.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.GetCultureInfo("fr-CH"));
    private static string Short(string value) => value.Length <= 8 ? value : value[..8];
}
