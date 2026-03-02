namespace Application.Mediator;

/// <summary>
/// Defines a handler for a specific mediator request type.
/// </summary>
/// <typeparam name="TRequest">Request type handled by this handler.</typeparam>
/// <typeparam name="TResponse">Response type returned by this handler.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the provided request and returns a response.
    /// </summary>
    /// <param name="request">Request instance to process.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Response produced by handling the request.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken ct = default);
}
