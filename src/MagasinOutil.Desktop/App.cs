using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using MagasinOutil.Core;
using MagasinOutil.Transport;

namespace MagasinOutil.Desktop;

public sealed class App : Application
{
    private NamedPipeProductClient? _client;
    private ProductSessionView? _session;
    private readonly string _clientId = "magasin8xx-hmi-" + Guid.NewGuid().ToString("N");

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = ThemeVariant.Light;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Le profil de démonstration intégré utilise un tube nommé Windows.");

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var pipe = Environment.GetEnvironmentVariable("WM_MAGASIN8XX_PIPE");
            _client = new NamedPipeProductClient(string.IsNullOrWhiteSpace(pipe) ? "wm.magasin8xx.demo" : pipe);
            ShowLogin(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    [SupportedOSPlatform("windows")]
    private void ShowLogin(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var login = new LoginWindow(SignInAsync, session => OpenProductAsync(desktop, session));
        login.Closed += (_, _) =>
        {
            if (ReferenceEquals(desktop.MainWindow, login))
            {
                _client?.Dispose();
                _client = null;
                desktop.Shutdown();
            }
        };
        desktop.MainWindow = login;
    }

    [SupportedOSPlatform("windows")]
    private async Task<ProductSignInResult> SignInAsync(string userName, string password)
    {
        if (_client is null)
            return new ProductSignInResult(ProductSignInStatus.Unavailable, null, "Client local indisponible.");

        return await _client.SignInAsync(new ProductSignInRequest(
            ProductSessionContract.Version,
            userName,
            password,
            _clientId));
    }

    [SupportedOSPlatform("windows")]
    private async Task OpenProductAsync(IClassicDesktopStyleApplicationLifetime desktop, ProductSessionView session)
    {
        if (_client is null) throw new InvalidOperationException("Client local indisponible.");

        var remoteMagazine = new RemoteMagazineService(
            _client,
            _client,
            _client,
            session.SessionReference,
            _clientId,
            session.Permissions);
        await remoteMagazine.RefreshAsync();
        if (!remoteMagazine.PlatformStatus.Connected || remoteMagazine.Read().Count == 0)
        {
            await _client.SignOutAsync(new ProductSessionRequest(
                ProductSessionContract.Version,
                session.SessionReference,
                _clientId));
            throw new InvalidOperationException("Le CoreHost n'a pas fourni de snapshot magasin exploitable.");
        }

        var login = desktop.MainWindow;
        _session = session;
        var main = new MainWindow(remoteMagazine);
        PlatformStatusOverlay.Attach(
            main,
            remoteMagazine,
            session,
            () =>
            {
                if (_client is null) return;
                var console = new PlatformConsoleWindow(_client, session, _clientId);
                _ = console.ShowDialog(main);
            });
        main.Closed += async (_, _) =>
        {
            if (_client is not null && _session is not null)
            {
                try
                {
                    await _client.SignOutAsync(new ProductSessionRequest(
                        ProductSessionContract.Version,
                        _session.SessionReference,
                        _clientId));
                }
                catch
                {
                    // Closing the local HMI must never block process shutdown.
                }
            }

            _session = null;
            _client?.Dispose();
            _client = null;
            desktop.Shutdown();
        };
        desktop.MainWindow = main;
        main.Show();
        login?.Close();
    }
}
