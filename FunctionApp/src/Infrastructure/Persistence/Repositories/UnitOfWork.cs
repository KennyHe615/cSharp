using Application.References;
using Application.Shared.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of the Unit of Work pattern using Entity Framework Core.
/// </summary>
/// <param name="dbContext">The database context used for tracking changes and persistence.</param>
public class UnitOfWork(FunctionAppDbContext.FunctionAppDbContext dbContext,
                        IServiceProvider serviceProvider) : IUnitOfWork
{
    /// <inheritdoc />
    public IReferencesRepository References => serviceProvider.GetRequiredService<IReferencesRepository>();

    /// <inheritdoc />
    public async Task UpsertAsync<TEntity>(TEntity entity,
                                           Action<TEntity>? onMissingFromIncoming = null,
                                           CancellationToken ct = default) where TEntity : class
    {
        await UpsertRangeAsync([entity], onMissingFromIncoming, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="EntityOperationException">Thrown when the entity type is not configured or a database error occurs.</exception>
    public async Task UpsertRangeAsync<TEntity>(IEnumerable<TEntity> incomingMappedEntities,
                                                Action<TEntity>? onMissingFromIncoming = null,
                                                CancellationToken ct = default) where TEntity : class
    {
        List<TEntity> incomingList = incomingMappedEntities.ToList();

        if (incomingList.Count == 0 && onMissingFromIncoming == null) return;

        string entityName = typeof(TEntity).Name;
        try
        {
            IEntityType entityType = dbContext.Model.FindEntityType(typeof(TEntity)) ??
                                     throw new EntityOperationException(
                                         $"Entity type '{entityName}' is not configured in the DbContext model.",
                                         entityName);
            IKey primaryKey = entityType.FindPrimaryKey() ??
                              throw new EntityOperationException(
                                  $"Primary key is not defined for entity '{entityName}'.",
                                  entityName);

            // Helper to extract the primary key value from an entity
            IProperty pkProperty = primaryKey.Properties[0];

            object? IdSelector(TEntity e)
            {
                return entityType.FindProperty(pkProperty.Name)!.GetGetter().GetClrValue(e);
            }

            // 1. Fetch existing entities in bulk to avoid N+1 FindAsync calls
            List<TEntity> dbEntities = await dbContext.Set<TEntity>().ToListAsync(ct).ConfigureAwait(false);
            Dictionary<object, TEntity> dbById = dbEntities.Select(e => (Id: IdSelector(e), Entity: e))
                                                           .Where(x => x.Id != null)
                                                           .ToDictionary(x => x.Id!, x => x.Entity);

            HashSet<object> incomingIds = [];

            // 2. Process Add and Update
            foreach (TEntity incoming in incomingList)
            {
                object? id = IdSelector(incoming);

                if (id == null) continue;
                incomingIds.Add(id);

                if (dbById.TryGetValue(id, out TEntity? existing))
                {
                    // Update: Copy values from incoming to existing tracked entity
                    dbContext.Entry(existing).CurrentValues.SetValues(incoming);
                }
                else
                {
                    // Add: New entity
                    await dbContext.Set<TEntity>().AddAsync(incoming, ct).ConfigureAwait(false);
                }
            }

            // 3. Process Missing (Inactivation/Sync)
            if (onMissingFromIncoming != null)
            {
                foreach (TEntity dbEntity in dbEntities.Where(e => IdSelector(e) != null &&
                                                                   !incomingIds.Contains(IdSelector(e)!)))
                {
                    onMissingFromIncoming(dbEntity);
                }
            }
        }
        catch (Exception ex) when (ex is not PersistenceException)
        {
            throw new EntityOperationException($"Failed to upsert entities of type '{entityName}'.", ex, entityName);
        }
    }

    /// <inheritdoc />
    /// <exception cref="DbConcurrencyException">Thrown when a concurrency conflict is detected.</exception>
    /// <exception cref="DbConstraintViolationException">Thrown when a database constraint (e.g., Foreign Key, Unique) is violated.</exception>
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new DbConcurrencyException(
                "A concurrency conflict occurred while saving changes. The data may have been modified by another user.",
                ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException != null)
        {
            string? constraintName = ExtractConstraintName(ex);

            throw new DbConstraintViolationException("A database constraint violation occurred while saving changes.",
                                                     ex,
                                                     constraintName);
        }
        catch (Exception ex) when (ex is not PersistenceException)
        {
            throw new EntityOperationException("An unexpected error occurred while saving changes to the database.",
                                               ex);
        }
    }

    /// <summary>
    /// Attempts to extract the name of the violated database constraint from the exception message.
    /// </summary>
    /// <param name="ex">The database update exception.</param>
    /// <returns>The extracted constraint name, or <see langword="null"/> if not found.</returns>
    private static string? ExtractConstraintName(DbUpdateException ex)
    {
        string? message = ex.InnerException?.Message;

        if (string.IsNullOrEmpty(message)) return null;

        int startIndex = message.IndexOf("constraint '", StringComparison.OrdinalIgnoreCase);

        if (startIndex == -1) return null;

        startIndex += 12;
        int endIndex = message.IndexOf('\'', startIndex);

        return endIndex > startIndex ? message.Substring(startIndex, endIndex - startIndex) : null;
    }
}
