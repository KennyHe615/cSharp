namespace Application.Mediator;

/// <summary>
/// Defines a lightweight mediator abstraction for dispatching typed requests to handlers.
/// </summary>
public interface ISimpleMediator
{
    /// <summary>
    /// Sends a request through the mediator pipeline and returns a response.
    /// </summary>
    /// <typeparam name="TResponse">Response type expected from the request handler.</typeparam>
    /// <param name="request">Request instance to dispatch.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Handler response for the provided request.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}
