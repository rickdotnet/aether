using System.Reflection;
using Aether.Abstractions.Messaging;
using RickDotNet.Base;

namespace Aether.Messaging;

public class AetherHandler
{
    private readonly Type? endpointType;
    private readonly IEndpointProvider? endpointProvider;
    private readonly Func<MessageContext, CancellationToken, Task>? handler;
    private readonly Dictionary<string, Type> subjectMapping = new();
    private readonly Dictionary<Type, EndpointMethod> handleMethods = new();

    private bool IsHandler => handler != null;

    public AetherHandler(Type endpointType, IEndpointProvider endpointProvider)
    {
        this.endpointType = endpointType;
        this.endpointProvider = endpointProvider;

        // get Handle methods from type
        PopulateHandleMethods();
    }

    public AetherHandler(Func<MessageContext, CancellationToken, Task> handler)
    {
        this.handler = handler;
    }

    private void PopulateHandleMethods()
    {
        // get all methods w/ "Handle" as the name
        //   - Handle(MessageType, MessageContext, CancellationToken)   // full
        //   - Handle(MessageType, CancellationToken)                   // slim method, no context
        //   - Handle(MessageContext, CancellationToken)                // fallback, context only 
        // MessageContext will hit in the second case

        // then populate the subjectMapping dictionary
        // with a string key of the message type

        if (endpointType == null) return;

        // Get all methods named "Handle"
        var methods = endpointType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "Handle");

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            EndpointMethod? endpointMethod = null;
            Type? messageType = null;

            switch (parameters.Length)
            {
                // Full signature: Handle(MessageType, MessageContext, CancellationToken)
                case 3 when
                    parameters[1].ParameterType == typeof(MessageContext) &&
                    parameters[2].ParameterType == typeof(CancellationToken):
                    
                    messageType = parameters[0].ParameterType;
                    endpointMethod = new EndpointMethod(method, MethodType.MessageTypeAndMessageContext);
                    break;
                // Slim signature: Handle(MessageType, CancellationToken)
                // Fallback signature: Handle(MessageContext, CancellationToken)
                case 2 when
                    parameters[1].ParameterType == typeof(CancellationToken):
                    
                    messageType = parameters[0].ParameterType;
                    endpointMethod = new EndpointMethod(method, MethodType.MessageType);
                    break;
                
                // shouldn't need to fa
                
                // // Fallback signature: Handle(MessageContext, CancellationToken)
                // case 2 when
                //     parameters[0].ParameterType == typeof(MessageContext) &&
                //     parameters[1].ParameterType == typeof(CancellationToken):
                //     
                //     messageType = typeof(MessageContext);
                //     endpointMethod = new EndpointMethod(method, MethodType.MessageContext);
                //     break;
            }

            if (endpointMethod != null && messageType != null)
            {
                // Map message type to a subject string (e.g., full type name or custom logic)
                subjectMapping[messageType.FullName!] = messageType;
                handleMethods[messageType] = endpointMethod;
            }
        }
    }

    public async Task<Result<VoidResult>> Handle(
        MessageContext context,
        CancellationToken cancellationToken)
    {
        if (IsHandler)
            return await Result.TryAsync(() => handler!(context, cancellationToken));

        if (endpointType == null || endpointProvider == null)
            return Result.Failure<VoidResult>("No endpoint or provider configured.");

        if (context.Headers.TryGetValue(MessageHeader.MessageTypeMapping, out var headerType) && headerType.Count > 0)
            context.Message.MessageType = subjectMapping[headerType.First()!];

        var messageType = context.Message.MessageType ?? typeof(MessageContext);
        var endpointMethod = handleMethods.GetValueOrDefault(messageType);
        if (endpointMethod == null)
            return Result.Failure<VoidResult>($"No Handle method found for type {messageType.Name}.");

        var endpointInstance = endpointProvider.GetService(endpointType);
        if (endpointInstance == null)
            return Result.Failure<VoidResult>($"Failed to resolve endpoint instance for {endpointType.Name}.");

        try
        {
            if (endpointMethod.HasReturnType && context.ReplyAvailable)
            {
                var response = await endpointMethod.InvokeResponse(endpointInstance, context, cancellationToken);
                if (response != null)
                {
                    await context.Reply(
                        AetherData.Serialize(response),
                        cancellationToken
                    );
                }
            }

            await endpointMethod.Invoke(endpointInstance, context, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure<VoidResult>($"Error invoking endpoint: {ex.Message}");
        }
    }

    private record EndpointMethod
    {
        public bool HasReturnType => MethodInfo.ReturnType.GetGenericArguments().Length > 0;
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
                MethodType.MessageType
                    => (Task)MethodInfo.Invoke(
                        endpointInstance,
                        [messageContext.Data.As(messageContext.Message.MessageType!), cancellationToken])!,
                MethodType.MessageTypeAndMessageContext
                    => (Task)MethodInfo.Invoke(endpointInstance,
                        [messageContext.Data.As(messageContext.Message.MessageType!), messageContext, cancellationToken])!,
                MethodType.MessageContext
                    => (Task)MethodInfo.Invoke(endpointInstance,
                        [messageContext, cancellationToken])!,

                _ => throw new InvalidOperationException()
            };
        }

        // Task<T> invoke result
        public dynamic InvokeResponse(object endpointInstance, MessageContext messageContext, CancellationToken cancellationToken)
        {
            return MethodType switch
            {
                MethodType.MessageType
                    => MethodInfo.Invoke(
                        endpointInstance,
                        [messageContext.Data.As(messageContext.Message.MessageType!), cancellationToken])!,
                MethodType.MessageTypeAndMessageContext
                    => MethodInfo.Invoke(endpointInstance,
                        [messageContext.Data.As(messageContext.Message.MessageType!), messageContext, cancellationToken])!,
                MethodType.MessageContext
                    => MethodInfo.Invoke(endpointInstance,
                        [messageContext, cancellationToken])!,

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
