using System.Reflection;
using Aether.Abstractions.Providers;
using RickDotNet.Base;

namespace Aether.Messaging;

public class EndpointInvoker
{
    private readonly Type? endpointType;
    private readonly IEndpointProvider? endpointProvider;
    private readonly Func<MessageContext, CancellationToken, Task>? handler;
    private readonly Dictionary<Type, EndpointMethod> handleMethods = new();

    private bool IsHandler => handler != null;

    public EndpointInvoker(Type endpointType, IEndpointProvider endpointProvider)
    {
        this.endpointType = endpointType;
        this.endpointProvider = endpointProvider;
    }

    public EndpointInvoker(Func<MessageContext, CancellationToken, Task> handler)
    {
        this.handler = handler;
    }

    public async Task<Result<VoidResult>> Invoke(Type messageType, MessageContext context, CancellationToken cancellationToken)
    {
        if (IsHandler)
        {
            var handlerResult = await Result.TryAsync(() => handler!(context, cancellationToken));
            return handlerResult;
        }

        if (endpointType is null)
            return Result.Failure("No endpoint type");

        var endpointInstance = endpointProvider!.GetService(endpointType);
        if (endpointInstance is null)
            return Result.Failure("No endpoint instance");

        var endpointMethod = GetEndpointMethod(messageType);
        if (endpointMethod is null)
            return Result.Failure("No endpoint method");

        var isRequest = messageType.IsRequest();

        if (isRequest)
        {
            if (!context.ReplyAvailable)
                return Result.Failure("No reply available");

            var response =
                await (dynamic)endpointMethod.Invoke(endpointInstance, context, cancellationToken);

            var data = AetherData.From(response);
            return await context.Reply(data, cancellationToken);
        }

        var endpointResult = await Result.TryAsync(() => endpointMethod.Invoke(endpointInstance, context, cancellationToken));
        return endpointResult;
    }

    private EndpointMethod? GetEndpointMethod(Type messageType)
    {
        var endpointMethod = handleMethods.GetValueOrDefault(messageType);
        if (endpointMethod != null)
            return endpointMethod;

        if(endpointType is null)
            return null;
        
        // should hit on MessageType, or MessageContext
        var handleMethod = endpointType.GetMethod("Handle", [messageType, typeof(CancellationToken)]);
        if (handleMethod != null)
        {
            var methodType = messageType == typeof(MessageContext)
                ? MethodType.MessageContext
                : MethodType.MessageType;
            endpointMethod = new EndpointMethod(handleMethod, methodType);
            handleMethods[messageType] = endpointMethod;

            return endpointMethod;
        }

        // should hit on MessageType and MessageContext
        handleMethod =
            endpointType.GetMethod("Handle", [messageType, typeof(MessageContext), typeof(CancellationToken)]);

        if (handleMethod == null)
            return null;

        endpointMethod = new EndpointMethod(handleMethod, MethodType.MessageTypeAndMessageContext);
        handleMethods[messageType] = endpointMethod;
        return endpointMethod;
    }
    
    private record EndpointMethod
    {
        private MethodInfo MethodInfo { get; }
        private MethodType MethodType { get; }

        public EndpointMethod(MethodInfo methodInfo, MethodType methodType)
        {
            MethodInfo = methodInfo;
            MethodType = methodType;
        }

        // invoke result, soon
        public Task Invoke(object endpointInstance, MessageContext messageContext, CancellationToken cancellationToken)
        {
            return MethodType switch
            {
                MethodType.MessageType => (Task)MethodInfo.Invoke(endpointInstance, [messageContext.Data.As(messageContext.Message.MessageType!), cancellationToken])!,
                MethodType.MessageTypeAndMessageContext => (Task)MethodInfo.Invoke(endpointInstance, [messageContext.Data.As(messageContext.Message.MessageType!), messageContext, cancellationToken])!,
                MethodType.MessageContext => (Task)MethodInfo.Invoke(endpointInstance, [messageContext, cancellationToken])!,
                _ => throw new InvalidOperationException()
            };
        }
    }

    private enum MethodType
    {
        MessageType,
        MessageContext,
        MessageTypeAndMessageContext,
    }
}
