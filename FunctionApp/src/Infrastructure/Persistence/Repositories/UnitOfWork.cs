using Application.Common.Abstractions.Context;
using Application.Common.Abstractions.Persistence;

using Infrastructure.Persistence.Repositories.UnitOfWorkCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

using Shared.Constants;


namespace Infrastructure.Persistence.Repositories;

public class UnitOfWork(FunctionAppDbContext.FunctionAppDbContext dbContext,
                        ILobContext lobContext,
                        ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private readonly string _lobName = lobContext.LobName;

    /// <inheritdoc />
    public async Task UpsertAsync<TEntity>(TEntity entity,
                                           Action<TEntity>? onMissingFromIncoming = null,
                                           CancellationToken ct = default) where TEntity : class
    {
        await UpsertRangeAsync([entity], onMissingFromIncoming, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="EntityOperationException">
    /// Thrown when the entity type is not configured or a database error occurs.
    /// </exception>
    public async Task UpsertRangeAsync<TEntity>(IEnumerable<TEntity> incomingMappedEntities,
                                                Action<TEntity>? onMissingFromIncoming = null,
                                                CancellationToken ct = default) where TEntity : class
    {
        List<TEntity> incomingList = incomingMappedEntities.ToList();

        if (incomingList.Count == 0 && onMissingFromIncoming == null) return;

        string entityName = typeof(TEntity).Name;

        try
        {
            EntityMetadata<TEntity> metadata = GetEntityMetadata<TEntity>(entityName);

            EntityValidator.ValidateIncomingData(incomingList, metadata, entityName, logger);

            List<TEntity> dbEntities = await EntityQueryBuilder
                                             .FetchMatchingEntitiesAsync(dbContext, incomingList, metadata, ct)
                                             .ConfigureAwait(false);

            Dictionary<object, TEntity> dbById = EntityValidator.BuildEntityDictionary(dbEntities, metadata);

            UpsertResult result =
                EntityUpdateHandler.ProcessUpsertOperations(dbContext, incomingList, dbById, metadata);

            logger.LogDebug(
                CommonConstants.LobEntityLogPrefix +
                "Processed: {AddedCount} added, {UpdatedCount} updated (Fetched {FetchedCount} existing from DB)",
                _lobName,
                entityName,
                result.AddedCount,
                result.UpdatedCount,
                dbEntities.Count);

            if (onMissingFromIncoming != null)
            {
                EntityUpdateHandler.ProcessMissingEntities(dbEntities,
                                                           result.IncomingKeys,
                                                           metadata,
                                                           onMissingFromIncoming);
            }
        }
        catch (Exception ex) when (ex is not PersistenceException)
        {
            string prefix = CommonConstants.LobEntityLogPrefix.Replace("{LobName}", _lobName)
                                           .Replace("{EntityName}", entityName);

            throw new EntityOperationException($"{prefix}Failed to upsert.", ex, entityName);
        }
    }

    /// <inheritdoc />
    /// <exception cref="DbConcurrencyException">Thrown when a concurrency conflict is detected.</exception>
    /// <exception cref="DbConstraintViolationException">
    /// Thrown when a database constraint (e.g., Foreign Key, Unique) is violated.
    /// </exception>
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

    #region ========== *** Private Methods *** ==========

    private EntityMetadata<TEntity> GetEntityMetadata<TEntity>(string entityName) where TEntity : class
    {
        string prefix = CommonConstants.LobEntityLogPrefix.Replace("{LobName}", _lobName)
                                       .Replace("{EntityName}", entityName);

        IEntityType entityType = dbContext.Model.FindEntityType(typeof(TEntity)) ??
                                 throw new EntityOperationException(
                                     $"{prefix} Is not configured in the DbContext model.");

        IKey primaryKey = entityType.FindPrimaryKey() ??
                          throw new EntityOperationException($"{prefix} Primary key is not defined.");

        return new EntityMetadata<TEntity>(entityType, primaryKey);
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

        string[] patterns =
        [
            "constraint '",
            "constraint \"",
            "CONSTRAINT '",
            "CONSTRAINT \"",
            "violates foreign key constraint \"",
            "violates unique constraint \""
        ];

        foreach (string pattern in patterns)
        {
            int startIndex = message.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);

            if (startIndex == -1) continue;

            startIndex += pattern.Length;
            char endChar = pattern.Contains('\'') ? '\'' : '\"';
            int endIndex = message.IndexOf(endChar, startIndex);

            if (endIndex > startIndex)
            {
                return message.Substring(startIndex, endIndex - startIndex);
            }
        }

        return null;
    }

    #endregion
}
