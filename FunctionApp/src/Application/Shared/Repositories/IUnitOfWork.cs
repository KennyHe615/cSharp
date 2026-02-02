namespace Application.Shared.Repositories;

/// <summary>
/// Defines the Unit of Work pattern for managing atomic database operations and entity persistence.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Performs an upsert (update or insert) operation for a single entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity, which must be a class.</typeparam>
    /// <param name="entity">The entity instance to upsert.</param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous upsert operation.</returns>
    Task UpsertAsync<TEntity>(TEntity entity, CancellationToken ct = default) where TEntity : class;

    /// <summary>
    /// Performs an upsert (update or insert) operation for a collection of entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entities, which must be a class.</typeparam>
    /// <param name="entities">The collection of entities to upsert.</param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous upsert range operation.</returns>
    Task UpsertRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken ct = default) where TEntity : class;

    /// <summary>
    /// Saves all changes made in this unit of work to the underlying database.
    /// </summary>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
