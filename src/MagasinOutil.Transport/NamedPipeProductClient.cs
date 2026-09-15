using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.Principal;
using Grpc.Core;
using Grpc.Net.Client;
using MagasinOutil.Core;
using Platform.Poc.Application.Contracts;
using Platform.Poc.Transport.Grpc;

namespace MagasinOutil.Transport;

[SupportedOSPlatform("windows")]
public sealed class NamedPipeProductClient : IProductSessionService, IToolInventoryReader, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly CallInvoker _invoker;

    public NamedPipeProductClient(string pipeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        _channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 4 * 1024 * 1024,
            MaxSendMessageSize = 16 * 1024,
            DisposeHttpClient = true,
            HttpHandler = new SocketsHttpHandler
            {
                ConnectCallback = async (_, cancellationToken) =>
                {
                    var stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut,
                        PipeOptions.Asynchronous | PipeOptions.WriteThrough, TokenImpersonationLevel.Anonymous);
                    try
                    {
                        await stream.ConnectAsync(cancellationToken).ConfigureAwait(false);
                        return stream;
                    }
                    catch
                    {
                        stream.Dispose();
                        throw;
                    }
                },
            },
        });
        _invoker = _channel.CreateCallInvoker();
    }

    public async ValueTask<ProductSignInResult> SignInAsync(ProductSignInRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var call = _invoker.AsyncUnaryCall(ProductSessionRpc.SignInMethod, null,
                new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: cancellationToken), request);
            return await call.ResponseAsync.ConfigureAwait(false);
        }
        catch (RpcException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (RpcException)
        {
            return new ProductSignInResult(ProductSignInStatus.Unavailable, null, "Service de session indisponible.");
        }
    }

    public async ValueTask<ProductSessionResult> ResolveAsync(ProductSessionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var call = _invoker.AsyncUnaryCall(ProductSessionRpc.ResolveMethod, null,
                new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: cancellationToken), request);
            return await call.ResponseAsync.ConfigureAwait(false);
        }
        catch (RpcException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (RpcException)
        {
            return new ProductSessionResult(ProductSessionStatus.Unavailable, null, "Service de session indisponible.");
        }
    }

    public async ValueTask<ProductSessionResult> SignOutAsync(ProductSessionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var call = _invoker.AsyncUnaryCall(ProductSessionRpc.SignOutMethod, null,
                new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: cancellationToken), request);
            return await call.ResponseAsync.ConfigureAwait(false);
        }
        catch (RpcException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (RpcException)
        {
            return new ProductSessionResult(ProductSessionStatus.Unavailable, null, "Service de session indisponible.");
        }
    }

    public async ValueTask<ToolInventorySnapshot> ReadAsync(ToolInventoryReadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var call = _invoker.AsyncUnaryCall(InventoryRpc.ReadMethod, null,
                new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: cancellationToken), request);
            return await call.ResponseAsync.ConfigureAwait(false);
        }
        catch (RpcException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (RpcException error)
        {
            var failure = error.StatusCode switch
            {
                StatusCode.FailedPrecondition => InventoryReadFailure.Incompatible,
                StatusCode.NotFound => InventoryReadFailure.WrongTarget,
                StatusCode.InvalidArgument => InventoryReadFailure.InvalidRequest,
                _ => InventoryReadFailure.Unavailable,
            };
            throw new InventoryReadException(failure, "Inventory read failed.", error);
        }
    }

    public void Dispose() => _channel.Dispose();
}
