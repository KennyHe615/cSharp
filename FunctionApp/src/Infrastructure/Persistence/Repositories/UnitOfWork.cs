using Application.Shared.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;


namespace Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of the Unit of Work pattern using Entity Framework Core.
/// </summary>
/// <param name="dbContext">The database context used for tracking changes and persistence.</param>
public class UnitOfWork(FunctionAppDbContext.FunctionAppDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc />
    public async Task UpsertAsync<TEntity>(TEntity entity, CancellationToken ct = default) where TEntity : class
    {
        await UpsertRangeAsync([entity], ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="EntityOperationException">Thrown when the entity type is not configured or a database error occurs.</exception>
    public async Task UpsertRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken ct = default)
        where TEntity : class
    {
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

            DbSet<TEntity> dbSet = dbContext.Set<TEntity>();

            foreach (TEntity entity in entities)
            {
                object?[] keyValues = primaryKey.Properties
                                                .Select(p => entityType.FindProperty(p.Name)!.GetGetter()
                                                                       .GetClrValue(entity))
                                                .ToArray();

                TEntity? existing = await dbSet.FindAsync(keyValues, ct).ConfigureAwait(false);

                if (existing == null)
                {
                    await dbSet.AddAsync(entity, ct).ConfigureAwait(false);
                }
                else
                {
                    dbContext.Entry(existing).CurrentValues.SetValues(entity);
                }
            }
        }
        catch (EntityOperationException)
        {
            throw;
        }
        catch (Exception ex)
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
