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
        Action openPlatform)
    {
        if (window.Content is not Control original) return;

        window.Content = null;
        var host = new Grid();
        host.Children.Add(original);

        var text = new TextBlock
        {
            FontSize = 12,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 860,
        };
        var badge = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#20304F")),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6),
            Child = text,
        };
        var platformButton = new Button
        {
            Content = "Plateforme",
            MinHeight = 38,
            Padding = new Thickness(14, 6),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        platformButton.Click += (_, _) => openPlatform();

        var surface = new StackPanel
        {
            Spacing = 6,
            Margin = new Thickness(16, 0, 16, 8),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        surface.Children.Add(platformButton);
        surface.Children.Add(badge);
        host.Children.Add(surface);
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
                    ? $"LICENCE ● · {license.LicenseId ?? "valide"}"
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
                text.Text = firstLine + Environment.NewLine + rightsLine + Environment.NewLine +
                    $"Dernière action · {command.Status} · {permission} · licence {command.LicenseStatus} · admission {command.AdmissionStatus} · Op {operation}";
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
