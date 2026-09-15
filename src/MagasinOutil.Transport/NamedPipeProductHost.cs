using System.Runtime.Versioning;
using MagasinOutil.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Platform.Poc.Application.Contracts;
using Platform.Poc.Transport.Grpc;

namespace MagasinOutil.Transport;

public static class NamedPipeProductHost
{
    [SupportedOSPlatform("windows")]
    public static WebApplication Create(
        string pipeName,
        IToolInventoryReader inventory,
        IProductSessionService sessions,
        IProductMagazineReadService magazineReads,
        IProductMagazineCommandService commands,
        IProductLicenseReadService licenses)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(magazineReads);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(licenses);

        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [] });
        builder.Configuration.Sources.Clear();
        builder.WebHost.UseNamedPipes(options => options.CurrentUserOnly = true);
        builder.WebHost.ConfigureKestrel(options =>
            options.ListenNamedPipe(pipeName, endpoint => endpoint.Protocols = HttpProtocols.Http2));
        builder.Services.AddSingleton(inventory);
        builder.Services.AddSingleton(sessions);
        builder.Services.AddSingleton(magazineReads);
        builder.Services.AddSingleton(commands);
        builder.Services.AddSingleton(licenses);
        builder.Services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = 64 * 1024;
            options.MaxSendMessageSize = 4 * 1024 * 1024;
            options.EnableDetailedErrors = false;
        });

        var app = builder.Build();
        // Legacy pilot read remains available for regression proof. The integrated HMI uses the
        // session-governed product services below.
        app.MapGrpcService<InventoryGrpcService>();
        app.MapGrpcService<ProductSessionGrpcService>();
        app.MapGrpcService<ProductMagazineGrpcService>();
        app.MapGrpcService<ProductCommandGrpcService>();
        return app;
    }
}
