using System.Collections.Concurrent;
using Aether.Abstractions.Messaging;
using Aether.Messaging;
using RickDotNet.Base;
using RickDotNet.Extensions.Base;

namespace Aether.Providers.Memory;

public class MemoryHub : IMessageHub
{
    private readonly ConcurrentDictionary<string, Func<MessageContext, CancellationToken, Task>> handlers = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<AetherData>> pendingRequests = new();
    private readonly CancellationTokenSource cts = new();
    private readonly TimeSpan requestTimeout;

    public MemoryHub(TimeSpan? requestTimeout = null)
    {
        this.requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(30);
    }

    public void AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler, CancellationToken cancellationToken) 
        => handlers[endpointConfig.FullSubject] = handler;

    public async Task<Result<VoidResult>> Send(AetherMessage message, CancellationToken cancellationToken = default)
    {
        if (!handlers.TryGetValue(message.Subject!, out var handler))
            return Result.Failure<VoidResult>("No channel registered for subject");

        var context = new MessageContext(
            message,
            replyFunc: (responseData, _) =>
            {
                if (!message.Headers.TryGetValue("request-id", out var requestId))
                    return Task.CompletedTask;

                if (pendingRequests.TryGetValue(requestId!, out var tcs))
                    tcs.TrySetResult(responseData);

                return Task.CompletedTask;
            });

        try
        {
            await handler(context, cancellationToken);
        }
        catch (Exception ex)
        {
            if (message.Headers.TryGetValue("request-id", out var requestId) &&
                pendingRequests.TryGetValue(requestId!, out var tcs))
            {
                tcs.TrySetException(ex);
            }
        }

        return Result.Success();
    }

    public async Task<Result<AetherData>> Request(AetherMessage message, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<AetherData>();

        if (!pendingRequests.TryAdd(requestId, tcs))
            return Result.Failure<AetherData>("Failed to register request");

        try
        {
            message.Headers["request-id"] = requestId;
            var dispatchResult = await DispatchMessage(message, cancellationToken);
            return await dispatchResult.SelectAsync(async _ =>
            {
                using var timeoutCts = new CancellationTokenSource(requestTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                var response = await tcs.Task.WaitAsync(linkedCts.Token);
                return response;
            });
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<AetherData>("Request timed out or was canceled");
        }
        catch (Exception ex)
        {
            return Result.Failure<AetherData>($"Request failed: {ex.Message}");
        }
        finally
        {
            pendingRequests.TryRemove(requestId, out _);
        }
    }

    private async Task<Result<VoidResult>> DispatchMessage(AetherMessage message, CancellationToken cancellationToken)
    {
        if (!handlers.TryGetValue(message.Subject!, out var handler))
            return Result.Failure<VoidResult>("No channel registered for subject");

        try
        {
            var context = new MessageContext(
                message,
                replyFunc: (responseData, _) =>
                {
                    if (!message.Headers.TryGetValue("request-id", out var requestId))
                        return Task.CompletedTask;

                    if (pendingRequests.TryGetValue(requestId!, out var tcs))
                        tcs.TrySetResult(responseData);

                    return Task.CompletedTask;
                });

            try
            {
                await handler(context, cancellationToken);
            }
            catch (Exception ex)
            {
                if (message.Headers.TryGetValue("request-id", out var requestId) &&
                    pendingRequests.TryGetValue(requestId!, out var tcs))
                {
                    tcs.TrySetException(ex);
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure<VoidResult>($"Failed to dispatch message: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await cts.CancelAsync();
        cts.Dispose();
    }
}
