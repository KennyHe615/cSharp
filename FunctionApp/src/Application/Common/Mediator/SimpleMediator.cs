using System.Reflection;

using Microsoft.Extensions.DependencyInjection;


namespace Application.Common.Mediator;

public class SimpleMediator(IServiceProvider serviceProvider) : ISimpleMediator
{
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request,
                                                 CancellationToken cancellationToken = default)
    {
        // Get the handler type for this request
        Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));

        // Get pipeline behaviors
        Type behaviorInterfaceType = typeof(IPipelineBehavior<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        List<object> behaviors = serviceProvider.GetServices(behaviorInterfaceType).Cast<object>().ToList();

        // Execute pipeline behaviors in reverse order (outermost first)
        RequestHandlerDelegate<TResponse> pipeline = behaviors
                                                     .OfType<IPipelineBehavior<IRequest<TResponse>, TResponse>>()
                                                     .Reverse()
                                                     .Aggregate((RequestHandlerDelegate<TResponse>)HandlerDelegate,
                                                                (next, pipeline) =>
                                                                    () => pipeline.Handle(
                                                                        request,
                                                                        next,
                                                                        cancellationToken));

        return await pipeline();

        // Create the handler delegate
        async Task<TResponse> HandlerDelegate()
        {
            object? handler = serviceProvider.GetService(handlerType);

            if (handler == null)
            {
                throw new InvalidOperationException($"No handler registered for request type {request.GetType()}");
            }

            MethodInfo? handleMethod = handler.GetType().GetMethod("Handle");
            if (handleMethod == null)
            {
                throw new InvalidOperationException($"Handler {handler.GetType()} does not have a Handle method");
            }

            object? result = handleMethod.Invoke(handler, [request, cancellationToken]);

            if (result is not Task task) return (TResponse)result!;

            await task.ConfigureAwait(false);
            PropertyInfo? resultProperty = task.GetType().GetProperty("Result");

            return (TResponse)(resultProperty?.GetValue(task) ?? default(TResponse)!);
        }
    }
}
