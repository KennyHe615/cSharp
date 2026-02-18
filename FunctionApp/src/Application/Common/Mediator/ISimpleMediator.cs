namespace Application.Common.Mediator;

public interface ISimpleMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}
