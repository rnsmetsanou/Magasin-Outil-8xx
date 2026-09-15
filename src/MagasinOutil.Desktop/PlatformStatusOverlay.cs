using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MagasinOutil.Core;

namespace MagasinOutil.Desktop;

internal static class PlatformStatusOverlay
{
    public static void Attach(MainWindow window, IIntegratedMagazineService magazine, ProductSessionView session)
    {
        if (window.Content is not Control original) return;

        window.Content = null;
        var host = new Grid();
        host.Children.Add(original);

        var text = new TextBlock
        {
            FontSize = 12,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.NoWrap,
        };
        var badge = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#20304F")),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 5),
            Margin = new Thickness(0, 0, 16, 8),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Child = text,
        };
        // The badge is added after the original content so it is rendered above it.
        host.Children.Add(badge);
        window.Content = host;

        void RefreshLabel()
        {
            var status = magazine.PlatformStatus;
            var core = status.Connected ? "CORE ●" : "CORE ○";
            var evidence = status.Connected
                ? $"{status.Quality ?? "?"} · {status.Freshness ?? "?"} · Machine #{status.MachineSessionGeneration?.ToString() ?? "?"}"
                : status.Message;
            text.Text = $"{session.DisplayName} · {core} · {evidence}";
            window.Title = $"Gestion des outils — WM — {session.DisplayName} · {core}";
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
}
