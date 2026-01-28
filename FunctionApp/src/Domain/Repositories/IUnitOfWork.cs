namespace Domain.Repositories;

public interface IUnitOfWork
{
    Task UpsertAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;

    Task UpsertRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
