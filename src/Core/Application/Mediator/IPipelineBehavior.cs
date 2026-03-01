namespace Application.Mediator;

/// <summary>
/// Defines a middleware component in the mediator pipeline that can run
/// before and/or after a request handler.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request and optionally delegates execution to the next pipeline component.
    /// </summary>
    /// <param name="request">Request instance.</param>
    /// <param name="next">Delegate that invokes the next component in the pipeline.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Response from the next component or from this behavior.</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct = default);
}

/// <summary>
/// Delegate that invokes the next pipeline component and returns a response.
/// </summary>
/// <typeparam name="TResponse">Response type.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
