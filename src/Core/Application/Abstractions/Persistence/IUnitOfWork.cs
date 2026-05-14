namespace Application.Abstractions.Persistence;

/// <summary>
/// Defines the Unit of Work pattern for managing atomic database operations and entity persistence.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Performs an upsert for one entity using the default property-copy update behavior when an existing row matches by primary key.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity, which must be a class.</typeparam>
    /// <param name="entity">The entity to upsert.</param>
    /// <param name="onMissingFromIncoming">
    /// Optional callback for items in the database not present in the incoming single-entity set.
    /// If provided, a full synchronization is performed for the entity type.
    /// </param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous upsert operation.</returns>
    Task UpsertAsync<TEntity>(TEntity entity,
                              Action<TEntity>? onMissingFromIncoming = null,
                              CancellationToken ct = default)
            where TEntity : class;

    /// <summary>
    /// Performs a high-performance upsert (update or insert) operation for a collection of entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entities, which must be a class.</typeparam>
    /// <param name="incomingMappedEntities">The collection of entities to upsert.</param>
    /// <param name="onMissingFromIncoming">Optional callback for items in the database not present in the incoming collection
    /// (e.g., for Inactivation or Soft-delete). If provided, a full synchronization is performed.</param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous upsert range operation.</returns>
    Task UpsertRangeAsync<TEntity>(IEnumerable<TEntity> incomingMappedEntities,
                                   Action<TEntity>? onMissingFromIncoming = null,
                                   CancellationToken ct = default)
            where TEntity : class;

    /// <summary>
    /// Performs an upsert for one entity using a caller-provided merge rule when an existing row matches by primary key.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity, which must be a class.</typeparam>
    /// <param name="entity">The entity to upsert.</param>
    /// <param name="onMatched">
    /// Callback applied when the incoming entity matches an existing tracked database entity by primary key.
    /// The first argument is the existing tracked entity; the second argument is the incoming entity.
    /// </param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous upsert operation.</returns>
    Task UpsertWithMergeAsync<TEntity>(TEntity entity,
                                       Action<TEntity, TEntity> onMatched,
                                       CancellationToken ct = default)
            where TEntity : class;

    /// <summary>
    /// Performs a range upsert using a caller-provided merge rule when existing rows match incoming rows by primary key.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entities, which must be a class.</typeparam>
    /// <param name="incomingMappedEntities">The collection of entities to upsert.</param>
    /// <param name="onMatched">
    /// Callback applied when an incoming entity matches an existing tracked database entity by primary key.
    /// The first argument is the existing tracked entity; the second argument is the incoming entity.
    /// </param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous upsert range operation.</returns>
    Task UpsertRangeWithMergeAsync<TEntity>(IEnumerable<TEntity> incomingMappedEntities,
                                            Action<TEntity, TEntity> onMatched,
                                            CancellationToken ct = default)
            where TEntity : class;

    /// <summary>
    /// Saves all changes made in this unit of work to the underlying database.
    /// </summary>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
