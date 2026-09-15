using System.Text.Json;
using Grpc.Core;
using MagasinOutil.Core;

namespace MagasinOutil.Transport;

public static class ProductCommandRpc
{
    private static Marshaller<T> Json<T>() => Marshallers.Create<T>(
        value => JsonSerializer.SerializeToUtf8Bytes(value),
        bytes => JsonSerializer.Deserialize<T>(bytes) ?? throw new InvalidDataException("Empty RPC payload."));

    public static readonly Method<ProductCommandRequest, ProductCommandResult> ExecuteMethod =
        new(MethodType.Unary, "wm.magasin8xx.v1.Commands", nameof(ProductCommandGrpcServiceBase.Execute),
            Json<ProductCommandRequest>(), Json<ProductCommandResult>());

    public static readonly Method<ProductSessionRequest, ProductLicenseView> ReadLicenseMethod =
        new(MethodType.Unary, "wm.magasin8xx.v1.Commands", nameof(ProductCommandGrpcServiceBase.ReadLicense),
            Json<ProductSessionRequest>(), Json<ProductLicenseView>());

    public static void BindService(ServiceBinderBase binder, ProductCommandGrpcServiceBase? service)
    {
        binder.AddMethod(ExecuteMethod, service is null ? null : service.Execute);
        binder.AddMethod(ReadLicenseMethod, service is null ? null : service.ReadLicense);
    }
}

[BindServiceMethod(typeof(ProductCommandRpc), nameof(ProductCommandRpc.BindService))]
public abstract class ProductCommandGrpcServiceBase
{
    public abstract Task<ProductCommandResult> Execute(ProductCommandRequest request, ServerCallContext context);
    public abstract Task<ProductLicenseView> ReadLicense(ProductSessionRequest request, ServerCallContext context);
}

public sealed class ProductCommandGrpcService(
    IProductMagazineCommandService commands,
    IProductLicenseReadService licenses) : ProductCommandGrpcServiceBase
{
    public override async Task<ProductCommandResult> Execute(ProductCommandRequest request, ServerCallContext context) =>
        await commands.ExecuteAsync(request, context.CancellationToken).ConfigureAwait(false);

    public override async Task<ProductLicenseView> ReadLicense(ProductSessionRequest request, ServerCallContext context) =>
        await licenses.ReadLicenseAsync(request, context.CancellationToken).ConfigureAwait(false);
}
