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

        window.Content = null;
        var host = new Grid { RowDefinitions = new("*,Auto") };
        Grid.SetRow(original, 0);
        host.Children.Add(original);

        var text = new TextBlock
        {
            FontSize = 11.5,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var platformButton = new Button
        {
            Content = "Plateforme",
            MinHeight = 38,
            MinWidth = 112,
            Padding = new Thickness(14, 6),
        };
        platformButton.Click += (_, _) => openPlatform();

        var switchButton = new Button
        {
            Content = "Changer d'utilisateur",
            MinHeight = 38,
            MinWidth = 170,
            Padding = new Thickness(14, 6),
        };
        switchButton.Click += async (_, _) =>
        {
            platformButton.IsEnabled = false;
            switchButton.IsEnabled = false;
            try
            {
                await switchUserAsync();
            }
            finally
            {
                platformButton.IsEnabled = true;
                switchButton.IsEnabled = true;
            }
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        buttons.Children.Add(platformButton);
        buttons.Children.Add(switchButton);

        var statusGrid = new Grid
        {
            ColumnDefinitions = new("*,Auto"),
            ColumnSpacing = 14,
        };
        Grid.SetColumn(text, 0);
        Grid.SetColumn(buttons, 1);
        statusGrid.Children.Add(text);
        statusGrid.Children.Add(buttons);

        var bar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#20304F")),
            Padding = new Thickness(14, 8),
            Child = statusGrid,
        };
        Grid.SetRow(bar, 1);
        host.Children.Add(bar);
        window.Content = host;

        void RefreshLabel()
        {
            var status = magazine.PlatformStatus;
            var core = status.Connected ? "CORE ●" : "CORE ○";
            var evidence = status.Connected
                ? $"{status.Quality ?? "?"}/{status.Freshness ?? "?"} · Machine #{status.MachineSessionGeneration?.ToString() ?? "?"}"
                : status.Message;
            var license = magazine.LicenseStatus;
            var licenseLabel = license is null
                ? "LICENCE ?"
                : license.Status == "Valid"
                    ? "LICENCE ●"
                    : $"LICENCE ○ · {license.Status}";

            var prepareRight = Right(magazine, "tool.prepare");
            var loadRight = Right(magazine, "tool.load");
            var editRight = Right(magazine, "tool.data.edit");
            var firstLine = $"{session.DisplayName} · {core} · {evidence} · {licenseLabel}";
            var rightsLine = $"Droits · Préparer {prepareRight} · Charger {loadRight} · Modifier {editRight}";
            var command = magazine.LastCommand;
            if (command is null)
            {
                text.Text = firstLine + Environment.NewLine + rightsLine;
            }
            else
            {
                var operation = string.IsNullOrWhiteSpace(command.OperationId) ? "—" : Short(command.OperationId);
                var permission = string.IsNullOrWhiteSpace(command.RequiredPermission)
                    ? "droit n/a"
                    : $"{command.RequiredPermission} {(command.PermissionGranted ? "✓" : "✕")}";
                text.Text = firstLine + Environment.NewLine + rightsLine +
                    $" · Dernière action {command.Status} · {permission} · admission {command.AdmissionStatus} · Op {operation}";
            }

            window.Title = $"Gestion des outils — WM — {session.DisplayName} · {core} · {licenseLabel}";
        }

        RefreshLabel();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        var refreshing = false;
        timer.Tick += async (_, _) =>
        {
            if (refreshing) return;
            refreshing = true;
            try
            {
                await magazine.RefreshAsync();
                RefreshLabel();
                window.InvalidateVisual();
            }
            catch
            {
                RefreshLabel();
            }
            finally
            {
                refreshing = false;
            }
        };
        timer.Start();
        window.Closed += (_, _) => timer.Stop();
    }

    private static string Right(IIntegratedMagazineService magazine, string permission) =>
        magazine.Permissions.Contains(permission) ? "✓" : "🔒";

    private static string Short(string value) => value.Length <= 8 ? value : value[..8];
}
