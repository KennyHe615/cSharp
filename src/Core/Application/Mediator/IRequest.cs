namespace Application.Mediator;

/// <summary>
/// Marker interface representing a mediator request with response type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">Response type produced by the request.</typeparam>
public interface IRequest<out TResponse>
{
}
