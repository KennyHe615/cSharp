namespace Infrastructure.Services;

public interface IMappedUpsertService
{
    Task UpsertAsync<TSource, TEntity>(IReadOnlyList<TSource> dtos, CancellationToken cancellationToken)
        where TEntity : class;
}
