using System.Globalization;
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

internal sealed class PlatformConsoleWindow : Window
{
    private readonly IProductAdministrationReadService _service;
    private readonly ProductSessionRequest _request;
    private readonly Grid _content = new() { RowDefinitions = new("Auto,*") };
    private readonly Bitmap _blueLogo = new(AssetLoader.Open(new Uri("avares://MagasinOutil.Desktop/Assets/wm-logo-blue.png")));
    private readonly Bitmap _whiteLogo = new(AssetLoader.Open(new Uri("avares://MagasinOutil.Desktop/Assets/wm-logo-white.png")));
    private readonly Image _logo = new() { Width = 148, Height = 58, Stretch = Stretch.Uniform };
    private readonly TextBlock _subtitle = new() { FontSize = 13, Opacity = 0.78, TextWrapping = TextWrapping.Wrap };
    private readonly Border _hero = new() { CornerRadius = new CornerRadius(10), Padding = new Thickness(18, 14) };
    private readonly bool _dark;

    private IBrush Ink => Brush(_dark ? "#FFFFFF" : "#202945");
    private IBrush MutedInk => Brush(_dark ? "#D7DEED" : "#5B667B");
    private IBrush Page => Brush(_dark ? "#051C2C" : "#F1F2F5");
    private IBrush Surface => Brush(_dark ? "#202945" : "#FFFFFF");
    private IBrush SurfaceAlt => Brush(_dark ? "#18263C" : "#F8F9FC");
    private IBrush Line => Brush(_dark ? "#32435F" : "#D8DEE9");
    private IBrush Accent => Brush(_dark ? "#AFC4FF" : "#264D91");
    private static IBrush Brush(string value) => new SolidColorBrush(Color.Parse(value));

    public PlatformConsoleWindow(
        IProductAdministrationReadService service,
        ProductSessionView session,
        string clientId)
    {
        _service = service;
        _request = new ProductSessionRequest(ProductSessionContract.Version, session.SessionReference, clientId);
        _dark = (Application.Current?.RequestedThemeVariant ?? ThemeVariant.Light) == ThemeVariant.Dark;

        Title = "Informations système — WM";
        Width = 1140;
        Height = 780;
        MinWidth = 980;
        MinHeight = 680;
        CanResize = true;
        UseLayoutRounding = true;
        RequestedThemeVariant = _dark ? ThemeVariant.Dark : ThemeVariant.Light;
        Background = Page;
        Foreground = Ink;
        _logo.Source = _dark ? _whiteLogo : _blueLogo;

        var root = new Grid { RowDefinitions = new("Auto,*") };
        root.Children.Add(BuildHeader());

        Grid.SetRow(_content, 1);
        _content.Margin = new Thickness(18, 0, 18, 18);
        _content.Children.Add(_hero);
        var loading = new Border
        {
            Child = new TextBlock
            {
                Text = "Chargement des informations système…",
                FontSize = 15,
                Foreground = MutedInk,
                Margin = new Thickness(6),
            },
        };
        _content.Children.Add(loading);
        Grid.SetRow(_hero, 0);
        Grid.SetRow(loading, 1);
        root.Children.Add(_content);

        Content = root;
        Closed += (_, _) => { _blueLogo.Dispose(); _whiteLogo.Dispose(); };
        Opened += async (_, _) => await LoadAsync();

        ApplyHero(
            "Console plateforme",
            "Cette fenêtre regroupe les informations secondaires de session, licence, utilisateurs et audit durable. L’écran métier reste volontairement plus léger.",
            StatusBadge("Session locale gouvernée", "Neutral"),
            StatusBadge("Données CoreHost", "Info"));
    }

    private Control BuildHeader()
    {
        var shell = new Border
        {
            Background = Surface,
            BorderBrush = Line,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(18, 14, 18, 12),
        };

        var header = new Grid { ColumnDefinitions = new("Auto,*,Auto"), ColumnSpacing = 16 };
        Grid.SetColumn(_logo, 0);
        header.Children.Add(_logo);

        var title = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        title.Children.Add(new TextBlock
        {
            Text = "Informations système",
            FontSize = 28,
            FontWeight = FontWeight.SemiBold,
            Foreground = Ink,
        });
        _subtitle.Text = "Plateforme multi-technologie · sécurité, licence et audit durable";
        _subtitle.Foreground = MutedInk;
        title.Children.Add(_subtitle);
        Grid.SetColumn(title, 1);
        header.Children.Add(title);

        var close = new Button
        {
            Content = "Fermer",
            MinHeight = 42,
            MinWidth = 118,
            Padding = new Thickness(16, 8),
            VerticalAlignment = VerticalAlignment.Center,
        };
        close.Click += (_, _) => Close();
        Grid.SetColumn(close, 2);
        header.Children.Add(close);

        shell.Child = header;
        return shell;
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
                "Le CoreHost local ne répond pas pour le moment.");
        }

        _content.Children.Clear();
        Grid.SetRow(_hero, 0);
        _content.Children.Add(_hero);

        if (!result.IsSuccess || result.Snapshot is not { } snapshot)
        {
            ApplyHero(
                "Données indisponibles",
                string.IsNullOrWhiteSpace(result.Reason)
                    ? "La console n’a pas pu charger les informations système."
                    : result.Reason,
                StatusBadge("CoreHost indisponible", "Danger"));

            var errorPanel = new StackPanel { Spacing = 8 };
            errorPanel.Children.Add(SectionTitle("Chargement impossible"));
            errorPanel.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(result.Reason)
                    ? "Aucune donnée d’administration n’a été retournée."
                    : result.Reason,
                TextWrapping = TextWrapping.Wrap,
                Foreground = MutedInk,
            });
            var error = SurfaceCard(errorPanel);
            Grid.SetRow(error, 1);
            _content.Children.Add(error);
            return;
        }

        ApplyHero(
            snapshot.Session.DisplayName,
            "Les détails système sont affichés ici pour éviter de surcharger la HMI métier. Les autorisations et l’audit restent fournis par le CoreHost.",
            StatusBadge(snapshot.License.Status == "Valid" ? "Licence active" : snapshot.License.Status, snapshot.License.Status == "Valid" ? "Success" : "Danger"),
            StatusBadge(snapshot.CanViewUsers ? "Gestion utilisateurs" : "Utilisateurs restreints", snapshot.CanViewUsers ? "Info" : "Neutral"),
            StatusBadge(snapshot.CanViewAudit ? "Audit durable visible" : "Audit restreint", snapshot.CanViewAudit ? "Info" : "Neutral"));

        var tabs = new TabControl
        {
            Margin = new Thickness(0, 14, 0, 0),
            ItemsSource = new[]
            {
                new TabItem { Header = "Vue générale", Content = Scroll(Overview(snapshot)) },
                new TabItem { Header = "Licence", Content = Scroll(License(snapshot.License)) },
                new TabItem { Header = "Utilisateurs", Content = Scroll(Users(snapshot)) },
                new TabItem { Header = "Audit durable", Content = Scroll(Audit(snapshot)) },
            },
        };
        Grid.SetRow(tabs, 1);
        _content.Children.Add(tabs);
    }

    private void ApplyHero(string heading, string message, params Control[] badges)
    {
        _hero.Background = Surface;
        _hero.BorderBrush = Line;
        _hero.BorderThickness = new Thickness(1);

        var layout = new StackPanel { Spacing = 10 };
        layout.Children.Add(new TextBlock
        {
            Text = heading,
            FontSize = 24,
            FontWeight = FontWeight.SemiBold,
            Foreground = Ink,
        });
        layout.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = MutedInk,
            FontSize = 14,
        });
        var badgesLine = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var badge in badges) badgesLine.Children.Add(badge);
        layout.Children.Add(badgesLine);
        _hero.Child = layout;
    }

    private Control Overview(ProductAdministrationSnapshot snapshot)
    {
        var root = new StackPanel { Spacing = 14, Margin = new Thickness(0, 2, 0, 2) };
        root.Children.Add(SectionCard(
            "Session courante",
            "Les informations ci-dessous viennent du CoreHost et représentent l’autorité de session.",
            OverviewGrid(
                ValueTile("Utilisateur", $"{snapshot.Session.DisplayName} ({snapshot.Session.UserName})"),
                ValueTile("Sujet", snapshot.Session.SubjectId),
                ValueTile("Émise", Format(snapshot.Session.IssuedAt)),
                ValueTile("Expiration", Format(snapshot.Session.AbsoluteExpiresAt)),
                ValueTile("Révision de politique", snapshot.Session.PolicyRevision.ToString(CultureInfo.InvariantCulture)),
                ValueTile("Périmètre", "Session locale gouvernée"))));

        var permissions = new StackPanel { Spacing = 10 };
        permissions.Children.Add(SectionTitle("Permissions effectives"));
        permissions.Children.Add(new TextBlock
        {
            Text = "Ces permissions sont appliquées par le CoreHost. L’interface ne décide pas de l’autorisation.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = MutedInk,
            FontSize = 13,
        });
        permissions.Children.Add(Chips(snapshot.Session.Permissions));
        root.Children.Add(SurfaceCard(permissions));

        var visibility = new StackPanel { Spacing = 10 };
        visibility.Children.Add(SectionTitle("Visibilité de cette console"));
        visibility.Children.Add(OverviewGrid(
            StatusTile("Utilisateurs", snapshot.CanViewUsers ? "Visible" : "Restreint", snapshot.CanViewUsers ? "Success" : "Neutral"),
            StatusTile("Audit durable", snapshot.CanViewAudit ? "Visible" : "Restreint", snapshot.CanViewAudit ? "Success" : "Neutral"),
            StatusTile("Licence", snapshot.License.Status == "Valid" ? "Active" : snapshot.License.Status, snapshot.License.Status == "Valid" ? "Success" : "Danger"),
            StatusTile("Machine", "Pilotée par le CoreHost", "Info")));
        root.Children.Add(SurfaceCard(visibility));
        return root;
    }

    private Control License(ProductLicenseView license)
    {
        var root = new StackPanel { Spacing = 14 };
        var top = new StackPanel { Spacing = 10 };
        top.Children.Add(SectionTitle("Licence produit"));
        top.Children.Add(new TextBlock
        {
            Text = "La licence est installée et évaluée par le runtime local. Les détails complets sont volontairement déplacés ici.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = MutedInk,
            FontSize = 13,
        });
        top.Children.Add(StatusBadge(license.Status == "Valid" ? "● Licence active" : "○ " + license.Status, license.Status == "Valid" ? "Success" : "Danger"));
        top.Children.Add(OverviewGrid(
            ValueTile("Installation", Empty(license.InstallationId)),
            ValueTile("Licence", Empty(license.LicenseId)),
            ValueTile("Renouvellement", license.RenewalVersion?.ToString(CultureInfo.InvariantCulture) ?? "—"),
            ValueTile("Valide depuis", license.ValidFromUtc is { } validFrom ? Format(validFrom) : "—"),
            ValueTile("Expiration", license.ExpiresAtUtc is { } expires ? Format(expires) : "Sans expiration déclarée"),
            ValueTile("État", license.Status)));
        root.Children.Add(SurfaceCard(top));

        var capabilities = new StackPanel { Spacing = 10 };
        capabilities.Children.Add(SectionTitle("Capacités signées"));
        capabilities.Children.Add(Chips(license.Capabilities));
        capabilities.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(license.Message) ? "Aucun commentaire complémentaire." : license.Message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = MutedInk,
            FontSize = 13,
        });
        root.Children.Add(SurfaceCard(capabilities));
        return root;
    }

    private Control Users(ProductAdministrationSnapshot snapshot)
    {
        var root = new StackPanel { Spacing = 14 };
        if (!snapshot.CanViewUsers)
        {
            var restricted = new StackPanel { Spacing = 10 };
            restricted.Children.Add(SectionTitle("Accès restreint"));
            restricted.Children.Add(Restricted("La consultation des comptes est réservée aux permissions identity.manage ou roles.manage."));
            restricted.Children.Add(new TextBlock
            {
                Text = "Votre propre session reste consultable dans la vue générale.",
                Foreground = MutedInk,
                FontSize = 13,
            });
            root.Children.Add(SurfaceCard(restricted));
            return root;
        }

        var intro = new StackPanel { Spacing = 8 };
        intro.Children.Add(SectionTitle("Catalogue des profils de démonstration"));
        intro.Children.Add(new TextBlock
        {
            Text = "Les profils ci-dessous sont fournis par le CoreHost local. Ils servent à démontrer la séparation entre administration et commandes machine.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = MutedInk,
            FontSize = 13,
        });
        root.Children.Add(SurfaceCard(intro));

        foreach (var user in snapshot.Users)
        {
            var card = new StackPanel { Spacing = 8 };
            var head = new Grid { ColumnDefinitions = new("*,Auto"), ColumnSpacing = 10 };
            head.Children.Add(new TextBlock
            {
                Text = user.DisplayName,
                FontSize = 18,
                FontWeight = FontWeight.SemiBold,
                Foreground = Ink,
            });
            var right = StatusBadge(user.IsEnabled ? "Actif" : "Désactivé", user.IsEnabled ? "Success" : "Danger");
            Grid.SetColumn(right, 1);
            head.Children.Add(right);
            card.Children.Add(head);
            card.Children.Add(new TextBlock { Text = $"Compte : {user.UserName}", Foreground = MutedInk, FontSize = 13 });
            card.Children.Add(new TextBlock { Text = $"Rôle : {user.Role}", Foreground = MutedInk, FontSize = 13 });
            card.Children.Add(Chips(user.Permissions));
            root.Children.Add(SurfaceCard(card));
        }

        var attention = new StackPanel { Spacing = 8 };
        attention.Children.Add(SectionTitle("Point d’attention"));
        attention.Children.Add(new TextBlock
        {
            Text = "Le rôle Administrateur n’obtient pas implicitement les actions machine tool.prepare et tool.load. L’administration et la conduite machine restent distinctes.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = MutedInk,
            FontSize = 13,
        });
        root.Children.Add(SurfaceCard(attention));
        return root;
    }

    private Control Audit(ProductAdministrationSnapshot snapshot)
    {
        var root = new StackPanel { Spacing = 14 };
        if (!snapshot.CanViewAudit)
        {
            var restricted = new StackPanel { Spacing = 10 };
            restricted.Children.Add(SectionTitle("Accès restreint"));
            restricted.Children.Add(Restricted("La lecture de l’audit durable est réservée à la permission audit.read."));
            root.Children.Add(SurfaceCard(restricted));
            return root;
        }

        var intro = new StackPanel { Spacing = 8 };
        intro.Children.Add(SectionTitle(snapshot.AuditScope));
        intro.Children.Add(new TextBlock
        {
            Text = "Cette vue montre les admissions persistées et qualifiées par D3. Elle ne remplace pas encore un journal cybersécurité complet.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = MutedInk,
            FontSize = 13,
        });
        root.Children.Add(SurfaceCard(intro));

        if (snapshot.AuditEntries.Count == 0)
        {
            root.Children.Add(SurfaceCard(new TextBlock
            {
                Text = "Aucune admission durable enregistrée pour le moment.",
                Foreground = MutedInk,
                FontSize = 14,
            }));
            return root;
        }

        foreach (var entry in snapshot.AuditEntries)
        {
            var card = new StackPanel { Spacing = 7 };
            var top = new Grid { ColumnDefinitions = new("*,Auto"), ColumnSpacing = 10 };
            top.Children.Add(new TextBlock
            {
                Text = entry.Action,
                FontSize = 17,
                FontWeight = FontWeight.SemiBold,
                Foreground = Ink,
            });
            var decision = StatusBadge(entry.Decision,
                entry.Decision.Contains("allow", StringComparison.OrdinalIgnoreCase) ||
                entry.Decision.Contains("commit", StringComparison.OrdinalIgnoreCase) ||
                entry.Decision.Contains("admit", StringComparison.OrdinalIgnoreCase)
                    ? "Success" : "Info");
            Grid.SetColumn(decision, 1);
            top.Children.Add(decision);
            card.Children.Add(top);
            card.Children.Add(OverviewGrid(
                ValueTile("Sujet", entry.SubjectId),
                ValueTile("Date", Format(entry.RecordedAtUtc)),
                ValueTile("Intent", Short(entry.IntentId)),
                ValueTile("Opération", Short(entry.OperationId)),
                ValueTile("Politique", entry.PolicyRevision.ToString(CultureInfo.InvariantCulture)),
                ValueTile("Nature", snapshot.AuditScope)));
            root.Children.Add(SurfaceCard(card));
        }
        return root;
    }

    private Border SectionCard(string title, string description, Control body)
    {
        var stack = new StackPanel { Spacing = 10 };
        stack.Children.Add(SectionTitle(title));
        stack.Children.Add(new TextBlock
        {
            Text = description,
            TextWrapping = TextWrapping.Wrap,
            Foreground = MutedInk,
            FontSize = 13,
        });
        stack.Children.Add(body);
        return SurfaceCard(stack);
    }

    private Control OverviewGrid(params Control[] items)
    {
        var grid = new UniformGrid { Columns = 2 };
        foreach (var item in items)
        {
            item.Margin = new Thickness(0, 0, 10, 10);
            grid.Children.Add(item);
        }
        return grid;
    }

    private Border ValueTile(string title, string value)
        => Tile(new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock { Text = title, FontSize = 12, Foreground = MutedInk },
                new TextBlock { Text = value, FontSize = 15, TextWrapping = TextWrapping.Wrap, Foreground = Ink },
            },
        });

    private Border StatusTile(string title, string value, string tone)
        => Tile(new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = title, FontSize = 12, Foreground = MutedInk },
                StatusBadge(value, tone),
            },
        });

    private Border Tile(Control child) => new()
    {
        Background = SurfaceAlt,
        BorderBrush = Line,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(12),
        Child = child,
    };

    private Border SurfaceCard(Control child) => new()
    {
        Background = Surface,
        BorderBrush = Line,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(16),
        Child = child,
    };

    private Border StatusBadge(string text, string tone)
    {
        var (background, foreground) = tone switch
        {
            "Success" => (_dark ? "#23443E" : "#E5F4ED", _dark ? "#A2E4C8" : "#205F49"),
            "Danger" => (_dark ? "#582C36" : "#FCE8EB", _dark ? "#FFB6C1" : "#972B42"),
            "Info" => (_dark ? "#273F67" : "#E5EDFF", _dark ? "#B9D1FF" : "#264D91"),
            _ => (_dark ? "#343E50" : "#EAECF0", _dark ? "#DCE1EB" : "#485366"),
        };
        return new Border
        {
            Background = Brush(background),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(10, 5),
            Margin = new Thickness(0, 0, 8, 8),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = text,
                Foreground = Brush(foreground),
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.NoWrap,
            },
        };
    }

    private Control Chips(IEnumerable<string> values)
    {
        var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var value in values.OrderBy(static value => value, StringComparer.Ordinal))
        {
            wrap.Children.Add(new Border
            {
                Background = SurfaceAlt,
                BorderBrush = Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(9, 5),
                Margin = new Thickness(0, 0, 8, 8),
                Child = new TextBlock { Text = value, Foreground = Accent, FontSize = 12 },
            });
        }
        if (wrap.Children.Count == 0)
            wrap.Children.Add(new TextBlock { Text = "Aucune", Foreground = MutedInk, FontSize = 13 });
        return wrap;
    }

    private static ScrollViewer Scroll(Control content) => new()
    {
        Content = content,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        Padding = new Thickness(0, 2, 0, 0),
    };

    private TextBlock SectionTitle(string text) => new()
    {
        Text = text,
        FontSize = 20,
        FontWeight = FontWeight.SemiBold,
        Foreground = Ink,
    };

    private Border Restricted(string message) => new()
    {
        Background = Brush(_dark ? "#4D3D22" : "#FFF1D7"),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(12),
        Child = new TextBlock
        {
            Text = "🔒 " + message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush(_dark ? "#FFDA96" : "#78500C"),
        },
    };

    private static string Empty(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
    private static string Format(DateTimeOffset value) => value.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.GetCultureInfo("fr-CH"));
    private static string Short(string value) => value.Length <= 8 ? value : value[..8];
}
