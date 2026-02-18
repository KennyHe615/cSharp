namespace Application.Common.Mediator;

public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct = default);
}

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
