using System.Text.Json;
using Grpc.Core;
using MagasinOutil.Core;

namespace MagasinOutil.Transport;

public static class ProductSessionRpc
{
    private static Marshaller<T> Json<T>() => Marshallers.Create<T>(
        value => JsonSerializer.SerializeToUtf8Bytes(value),
        bytes => JsonSerializer.Deserialize<T>(bytes) ?? throw new InvalidDataException("Empty RPC payload."));

    public static readonly Method<ProductSignInRequest, ProductSignInResult> SignInMethod =
        new(MethodType.Unary, "wm.magasin8xx.v1.Session", nameof(ProductSessionGrpcServiceBase.SignIn),
            Json<ProductSignInRequest>(), Json<ProductSignInResult>());

    public static readonly Method<ProductSessionRequest, ProductSessionResult> ResolveMethod =
        new(MethodType.Unary, "wm.magasin8xx.v1.Session", nameof(ProductSessionGrpcServiceBase.Resolve),
            Json<ProductSessionRequest>(), Json<ProductSessionResult>());

    public static readonly Method<ProductSessionRequest, ProductSessionResult> SignOutMethod =
        new(MethodType.Unary, "wm.magasin8xx.v1.Session", nameof(ProductSessionGrpcServiceBase.SignOut),
            Json<ProductSessionRequest>(), Json<ProductSessionResult>());

    public static void BindService(ServiceBinderBase binder, ProductSessionGrpcServiceBase? service)
    {
        binder.AddMethod(SignInMethod, service is null ? null : service.SignIn);
        binder.AddMethod(ResolveMethod, service is null ? null : service.Resolve);
        binder.AddMethod(SignOutMethod, service is null ? null : service.SignOut);
    }
}

[BindServiceMethod(typeof(ProductSessionRpc), nameof(ProductSessionRpc.BindService))]
public abstract class ProductSessionGrpcServiceBase
{
    public abstract Task<ProductSignInResult> SignIn(ProductSignInRequest request, ServerCallContext context);
    public abstract Task<ProductSessionResult> Resolve(ProductSessionRequest request, ServerCallContext context);
    public abstract Task<ProductSessionResult> SignOut(ProductSessionRequest request, ServerCallContext context);
}

public sealed class ProductSessionGrpcService(IProductSessionService sessions) : ProductSessionGrpcServiceBase
{
    public override Task<ProductSignInResult> SignIn(ProductSignInRequest request, ServerCallContext context) =>
        sessions.SignInAsync(request, context.CancellationToken).AsTask();

    public override Task<ProductSessionResult> Resolve(ProductSessionRequest request, ServerCallContext context) =>
        sessions.ResolveAsync(request, context.CancellationToken).AsTask();

    public override Task<ProductSessionResult> SignOut(ProductSessionRequest request, ServerCallContext context) =>
        sessions.SignOutAsync(request, context.CancellationToken).AsTask();
}
