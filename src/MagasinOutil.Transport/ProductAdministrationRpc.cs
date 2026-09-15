using System.Text.Json;
using Grpc.Core;
using MagasinOutil.Core;

namespace MagasinOutil.Transport;

public static class ProductAdministrationRpc
{
    private static Marshaller<T> Json<T>() => Marshallers.Create<T>(
        value => JsonSerializer.SerializeToUtf8Bytes(value),
        bytes => JsonSerializer.Deserialize<T>(bytes) ?? throw new InvalidDataException("Empty RPC payload."));

    public static readonly Method<ProductSessionRequest, ProductAdministrationSnapshot?> ReadMethod =
        new(MethodType.Unary, "wm.magasin8xx.v1.Administration", nameof(ProductAdministrationGrpcServiceBase.Read),
            Json<ProductSessionRequest>(), Json<ProductAdministrationSnapshot?>());

    public static void BindService(ServiceBinderBase binder, ProductAdministrationGrpcServiceBase? service) =>
        binder.AddMethod(ReadMethod, service is null ? null : service.Read);
}

[BindServiceMethod(typeof(ProductAdministrationRpc), nameof(ProductAdministrationRpc.BindService))]
public abstract class ProductAdministrationGrpcServiceBase
{
    public abstract Task<ProductAdministrationSnapshot?> Read(ProductSessionRequest request, ServerCallContext context);
}

public sealed class ProductAdministrationGrpcService(IProductAdministrationReadService service) : ProductAdministrationGrpcServiceBase
{
    public override async Task<ProductAdministrationSnapshot?> Read(ProductSessionRequest request, ServerCallContext context) =>
        await service.ReadAdministrationAsync(request, context.CancellationToken).ConfigureAwait(false);
}
