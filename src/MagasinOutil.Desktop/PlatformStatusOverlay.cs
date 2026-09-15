using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MagasinOutil.Core;

namespace MagasinOutil.Desktop;

internal static class PlatformStatusOverlay
{
    public static void Attach(
        MainWindow window,
        IIntegratedMagazineService magazine,
        ProductSessionView session,
        Action openPlatform,
        Func<Task> switchUserAsync)
    {
        if (window.Content is not Control original) return;

        var infoButton = new Button
        {
            Content = "ⓘ",
            MinWidth = 44,
            MinHeight = 44,
            Padding = new Thickness(8),
            FontSize = 19,
        };
        ToolTip.SetTip(infoButton, "À propos / informations Plateforme");
        infoButton.Click += (_, _) => openPlatform();

        var identityText = new StackPanel { Spacing = 0, VerticalAlignment = VerticalAlignment.Center };
        identityText.Children.Add(new TextBlock
        {
            Text = session.DisplayName,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.NoWrap,
        });
        identityText.Children.Add(new TextBlock
        {
            Text = session.UserName,
            FontSize = 10.5,
            Opacity = 0.68,
            TextWrapping = TextWrapping.NoWrap,
        });

        var identityContent = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 7,
            VerticalAlignment = VerticalAlignment.Center,
        };
        identityContent.Children.Add(new TextBlock
        {
            Text = "👤",
            FontSize = 18,
            VerticalAlignment = VerticalAlignment.Center,
        });
        identityContent.Children.Add(identityText);

        var identity = new Border
        {
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#AAB4C5")),
            Padding = new Thickness(10, 4),
            MinHeight = 44,
            MinWidth = 138,
            Child = identityContent,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(identity, $"Session ouverte pour {session.DisplayName} ({session.UserName})");

        var switchButton = new Button
        {
            Content = "↻",
            MinWidth = 44,
            MinHeight = 44,
            Padding = new Thickness(8),
            FontSize = 20,
        };
        ToolTip.SetTip(switchButton, "Changer d’utilisateur");
        switchButton.Click += async (_, _) =>
        {
            infoButton.IsEnabled = false;
            switchButton.IsEnabled = false;
            try
            {
                await switchUserAsync();
            }
            finally
            {
                infoButton.IsEnabled = true;
                switchButton.IsEnabled = true;
            }
        };

        window.AddHeaderAction(infoButton);
        window.AddHeaderAction(identity);
        window.AddHeaderAction(switchButton);

        window.Content = null;
        var host = new Grid { RowDefinitions = new("*,Auto") };
        Grid.SetRow(original, 0);
        host.Children.Add(original);

        var coreText = StatusText();
        var machineText = StatusText();
        var licenseText = StatusText();
        var actionText = StatusText();

        var live = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 22,
            VerticalAlignment = VerticalAlignment.Center,
        };
        live.Children.Add(coreText);
        live.Children.Add(machineText);
        live.Children.Add(licenseText);
        live.Children.Add(actionText);

        var bar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#20304F")),
            BorderBrush = new SolidColorBrush(Color.Parse("#344665")),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 7),
            MinHeight = 38,
            Child = live,
        };
        Grid.SetRow(bar, 1);
        host.Children.Add(bar);
        window.Content = host;
        window.Title = "Gestion des outils — WM";

        void RefreshStatus()
        {
            var status = magazine.PlatformStatus;
            var machineHealthy = status.Connected &&
                                 string.Equals(status.Quality, "Good", StringComparison.Ordinal) &&
                                 string.Equals(status.Freshness, "Fresh", StringComparison.Ordinal);
            var license = magazine.LicenseStatus;
            var licenseHealthy = license?.Status == "Valid";

            coreText.Text = status.Connected ? "● CORE connecté" : "○ CORE déconnecté";
            coreText.Foreground = StatusBrush(status.Connected);
            ToolTip.SetTip(coreText, status.Message);

            machineText.Text = status.Connected
                ? $"● Machine {status.Quality ?? "?"}/{status.Freshness ?? "?"}"
                : "○ Machine indisponible";
            machineText.Foreground = StatusBrush(machineHealthy);
            ToolTip.SetTip(machineText,
                status.Connected
                    ? $"Session Machine #{status.MachineSessionGeneration?.ToString() ?? "?"} · {status.Origin ?? "origine inconnue"}"
                    : status.Message);

            licenseText.Text = licenseHealthy ? "● Licence valide" : $"○ Licence {license?.Status ?? "inconnue"}";
            licenseText.Foreground = StatusBrush(licenseHealthy);
            ToolTip.SetTip(licenseText,
                license is null
                    ? "État de licence indisponible."
                    : $"Licence {license.LicenseId ?? "—"} · {license.Message}");

            var command = magazine.LastCommand;
            if (command is null)
            {
                actionText.Text = "Prêt";
                actionText.Foreground = Brushes.White;
                ToolTip.SetTip(actionText, "Aucune commande gouvernée soumise dans cette session.");
            }
            else
            {
                var action = ActionLabel(command.RequiredPermission);
                var completed = command.Status is ProductCommandStatus.Completed or ProductCommandStatus.AlreadyAdmitted;
                actionText.Text = completed ? $"✓ {action}" : $"⚠ {action} · {ShortStatus(command.Status)}";
                actionText.Foreground = StatusBrush(completed);
                var operation = string.IsNullOrWhiteSpace(command.OperationId) ? "—" : command.OperationId;
                ToolTip.SetTip(actionText,
                    $"{command.Message}\nPermission : {command.RequiredPermission}\nLicence : {command.LicenseStatus}\nAdmission : {command.AdmissionStatus}\nOperation : {operation}");
            }
        }

        RefreshStatus();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        var refreshing = false;
        timer.Tick += async (_, _) =>
        {
            if (refreshing) return;
            refreshing = true;
            try
            {
                await magazine.RefreshAsync();
                RefreshStatus();
                window.InvalidateVisual();
            }
            catch
            {
                RefreshStatus();
            }
            finally
            {
                refreshing = false;
            }
        };
        timer.Start();
        window.Closed += (_, _) => timer.Stop();
    }

    private static TextBlock StatusText() => new()
    {
        FontSize = 12,
        Foreground = Brushes.White,
        TextWrapping = TextWrapping.NoWrap,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static IBrush StatusBrush(bool healthy) =>
        new SolidColorBrush(Color.Parse(healthy ? "#A2E4C8" : "#FFB6C1"));

    private static string ActionLabel(string permission) => permission switch
    {
        "tool.prepare" => "Préparer",
        "tool.load" => "Charger",
        "tool.data.edit" => "Modifier",
        _ => "Action",
    };

    private static string ShortStatus(ProductCommandStatus status) => status switch
    {
        ProductCommandStatus.MissingPermission => "droit refusé",
        ProductCommandStatus.LicenseRejected => "licence refusée",
        ProductCommandStatus.BusinessRejected => "condition refusée",
        ProductCommandStatus.AdmissionConflict => "conflit",
        ProductCommandStatus.SessionInvalid => "session invalide",
        _ => status.ToString(),
    };
}
