using System.Text.Json;
using Grpc.Core;
using MagasinOutil.Core;

namespace MagasinOutil.Transport;

public static class ProductMagazineRpc
{
    private static Marshaller<T> Json<T>() => Marshallers.Create<T>(
        value => JsonSerializer.SerializeToUtf8Bytes(value),
        bytes => JsonSerializer.Deserialize<T>(bytes) ?? throw new InvalidDataException("Empty RPC payload."));

    public static readonly Method<ProductMagazineReadRequest, ProductMagazineReadResult> ReadMethod =
        new(MethodType.Unary, "wm.magasin8xx.v1.Magazine", nameof(ProductMagazineGrpcServiceBase.Read),
            Json<ProductMagazineReadRequest>(), Json<ProductMagazineReadResult>());

    public static void BindService(ServiceBinderBase binder, ProductMagazineGrpcServiceBase? service) =>
        binder.AddMethod(ReadMethod, service is null ? null : service.Read);
}

[BindServiceMethod(typeof(ProductMagazineRpc), nameof(ProductMagazineRpc.BindService))]
public abstract class ProductMagazineGrpcServiceBase
{
    public abstract Task<ProductMagazineReadResult> Read(ProductMagazineReadRequest request, ServerCallContext context);
}

public sealed class ProductMagazineGrpcService(IProductMagazineReadService service) : ProductMagazineGrpcServiceBase
{
    public override async Task<ProductMagazineReadResult> Read(ProductMagazineReadRequest request, ServerCallContext context) =>
        await service.ReadAsync(request, context.CancellationToken).ConfigureAwait(false);
}
