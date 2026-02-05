using Application.References;


namespace Application.Shared.Repositories;

/// <summary>
/// Defines the Unit of Work pattern for managing atomic database operations and entity persistence.
/// </summary>
public interface IUnitOfWork
{
    // Category-specific repository access
    IReferencesRepository References { get; }

    Task UpsertAsync<TEntity>(TEntity entity,
                              Action<TEntity>? onMissingFromIncoming = null,
                              CancellationToken ct = default) where TEntity : class;

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
                                   CancellationToken ct = default) where TEntity : class;

    // /// <summary>
    // /// Synchronizes a collection of entities by comparing them with the database.
    // /// Handles Adding, Updating, and a callback for "Missing" items (Inactivation).
    // /// </summary>
    // Task SyncAsync<TEntity>(IEnumerable<TEntity> incomingMappedEntities,
    //                         Func<TEntity, Guid> idSelector,
    //                         Action<TEntity, TEntity> updateAction,
    //                         Action<TEntity>? inactivateAction = null,
    //                         CancellationToken ct = default) where TEntity : class;

    /// <summary>
    /// Saves all changes made in this unit of work to the underlying database.
    /// </summary>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
