using Application.Abstractions.Context;
using Application.Abstractions.Persistence;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Repositories.UnitOfWorkCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

using SharedKernel.Lobs;
using SharedKernel.Logging;
using SharedKernel.Time;


namespace Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext dbContext,
                               ILobContext lobContext,
                               IDateTimeProvider dateTimeProvider,
                               ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private const string CategoryName = "Persistence.UnitOfWork";
    private readonly LobName _lobName = lobContext.LobName;

    public Task UpsertAsync<TEntity>(TEntity entity,
                                     Action<TEntity>? onMissingFromIncoming = null,
                                     CancellationToken ct = default)
            where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        return UpsertRangeCoreAsync([entity],
                                    null,
                                    onMissingFromIncoming,
                                    ct);
    }

    public Task UpsertRangeAsync<TEntity>(IEnumerable<TEntity> incomingMappedEntities,
                                          Action<TEntity>? onMissingFromIncoming = null,
                                          CancellationToken ct = default)
            where TEntity : class
    {
        return UpsertRangeCoreAsync(incomingMappedEntities,
                                    null,
                                    onMissingFromIncoming,
                                    ct);
    }

    public Task UpsertWithMergeAsync<TEntity>(TEntity entity,
                                              Action<TEntity, TEntity> onMatched,
                                              CancellationToken ct = default)
            where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(onMatched);

        return UpsertRangeCoreAsync([entity],
                                    onMatched,
                                    null,
                                    ct);
    }

    public Task UpsertRangeWithMergeAsync<TEntity>(IEnumerable<TEntity> incomingMappedEntities,
                                                   Action<TEntity, TEntity> onMatched,
                                                   CancellationToken ct = default)
            where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(onMatched);

        return UpsertRangeCoreAsync(incomingMappedEntities,
                                    onMatched,
                                    null,
                                    ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        using IDisposable _ = logger.BeginOperationScope(_lobName, CategoryName);

        try
        {
            return await dbContext.SaveChangesAsync(ct)
                                  .ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new
                    DbConcurrencyException("A concurrency conflict occurred while saving changes. Data may have been modified by another process.",
                                           ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is not null)
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

    private async Task UpsertRangeCoreAsync<TEntity>(IEnumerable<TEntity> incomingMappedEntities,
                                                     Action<TEntity, TEntity>? onMatched,
                                                     Action<TEntity>? onMissingFromIncoming,
                                                     CancellationToken ct)
            where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(incomingMappedEntities);

        List<TEntity> incomingList = incomingMappedEntities.ToList();

        if (incomingList.Count == 0 && onMissingFromIncoming is null) return;

        string entityName = typeof(TEntity).Name;

        using IDisposable _ = logger.BeginOperationScope(_lobName, CategoryName, entityName);

        try
        {
            EntityMetadata<TEntity> metadata = GetEntityMetadata<TEntity>(entityName);

            EntityValidator.ValidateIncomingData(incomingList,
                                                 metadata,
                                                 _lobName,
                                                 entityName,
                                                 logger);

            List<TEntity> dbMatchedEntities = await EntityQueryBuilder
                                                   .FetchMatchingEntitiesAsync(dbContext,
                                                                               incomingList,
                                                                               metadata,
                                                                               ct)
                                                   .ConfigureAwait(false);

            Dictionary<object, TEntity> dbById = EntityValidator.BuildEntityDictionary(dbMatchedEntities, metadata);

            UpsertResult result = EntityUpdateHandler.ProcessUpsertOperations(dbContext,
                                                                              incomingList,
                                                                              dbById,
                                                                              metadata,
                                                                              onMatched);

            logger.LogDebug(LobLogTemplates.LobCategoryEntity
                            + "Upsert processed | Added={AddedCount} Updated={UpdatedCount} MatchedFetched={FetchedCount}",
                            _lobName,
                            CategoryName,
                            entityName,
                            result.AddedCount,
                            result.UpdatedCount,
                            dbMatchedEntities.Count);

            if (onMissingFromIncoming is not null)
            {
                List<TEntity> dbAllEntities = await dbContext.Set<TEntity>()
                                                             .AsTracking()
                                                             .ToListAsync(ct)
                                                             .ConfigureAwait(false);

                EntityUpdateHandler.ProcessMissingEntities(dbAllEntities,
                                                           result.IncomingKeys,
                                                           metadata,
                                                           onMissingFromIncoming);
            }
        }
        catch (Exception ex) when (ex is not PersistenceException)
        {
            logger.LogErrorWithDetails(ex,
                                       LobLogTemplates.LobCategoryEntity + "Upsert failed.",
                                       _lobName,
                                       CategoryName,
                                       entityName);

            throw new EntityOperationException($"[{_lobName}] [{entityName}] Failed to upsert range.", ex, entityName);
        }
    }

    private EntityMetadata<TEntity> GetEntityMetadata<TEntity>(string entityName)
            where TEntity : class
    {
        IEntityType entityType =
                dbContext.Model.FindEntityType(typeof(TEntity))
                ?? throw new
                        EntityOperationException($"[{_lobName}] [{entityName}] Entity is not configured in DbContext model.");

        IKey primaryKey =
                entityType.FindPrimaryKey()
                ?? throw new EntityOperationException($"[{_lobName}] [{entityName}] Primary key is not defined.");

        return new EntityMetadata<TEntity>(entityType, primaryKey, dateTimeProvider);
    }

    private static string? ExtractConstraintName(DbUpdateException ex)
    {
        string? message = ex.InnerException?.Message;

        if (string.IsNullOrWhiteSpace(message)) return null;

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

            if (startIndex < 0) continue;

            startIndex += pattern.Length;
            char endChar = pattern.Contains('\'') ? '\'' : '"';
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
