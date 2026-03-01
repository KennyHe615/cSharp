using System.Reflection;
using System.Runtime.ExceptionServices;

using Microsoft.Extensions.DependencyInjection;


namespace Application.Mediator;

/// <summary>
/// Default mediator implementation that resolves handlers and pipeline behaviors from DI
/// and executes them in a composed request pipeline.
/// </summary>
internal sealed class SimpleMediator(IServiceProvider serviceProvider) : ISimpleMediator
{
    /// <inheritdoc />
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Type requestType = request.GetType();
        Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        Type behaviorInterfaceType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));

        List<object> behaviors = serviceProvider.GetServices(behaviorInterfaceType)
                                                .Cast<object>()
                                                .ToList();

        RequestHandlerDelegate<TResponse> pipeline = () => InvokeHandlerAsync(request, handlerType, ct);

        foreach (object behavior in behaviors.AsEnumerable()
                                             .Reverse())
        {
            RequestHandlerDelegate<TResponse> next = pipeline;

            pipeline = () =>
                       {
                           MethodInfo? handleMethod = behavior.GetType()
                                                              .GetMethod("Handle");
                           if (handleMethod is null)
                           {
                               throw new
                                   InvalidOperationException($"Pipeline behavior '{behavior.GetType().Name}' does not have a Handle method.");
                           }

                           object? result;
                           try
                           {
                               result = handleMethod.Invoke(behavior, [request, next, ct]);
                           }
                           catch (TargetInvocationException ex) when (ex.InnerException is not null)
                           {
                               ExceptionDispatchInfo.Capture(ex.InnerException)
                                                    .Throw();

                               throw;
                           }

                           return result as Task<TResponse>
                                  ?? throw new
                                      InvalidOperationException($"Pipeline behavior '{behavior.GetType().Name}' returned an invalid result.");
                       };
        }

        return await pipeline()
           .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Resolves and invokes the concrete request handler for the provided request type.
    /// </summary>
    /// <typeparam name="TResponse">Response type returned by the handler.</typeparam>
    /// <param name="request">Request instance being handled.</param>
    /// <param name="handlerType">Closed generic handler service type.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Handler response.</returns>
    private async Task<TResponse> InvokeHandlerAsync<TResponse>(IRequest<TResponse> request,
                                                                Type handlerType,
                                                                CancellationToken ct)
    {
        object handler = serviceProvider.GetRequiredService(handlerType);

        MethodInfo? handleMethod = handler.GetType()
                                          .GetMethod("Handle");
        if (handleMethod is null)
        {
            throw new InvalidOperationException($"Handler '{handler.GetType().Name}' does not have a Handle method.");
        }

        object? result;
        try
        {
            result = handleMethod.Invoke(handler, [request, ct]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException)
                                 .Throw();

            throw;
        }

        if (result is Task<TResponse> typedTask)
        {
            return await typedTask.ConfigureAwait(false);
        }

        throw new InvalidOperationException($"Handler '{handler.GetType().Name}' returned an invalid result.");
    }

    #endregion
}
