using Microsoft.Extensions.Logging;

using SharedKernel.Lobs;
using SharedKernel.Logging;


namespace Infrastructure.Persistence.Repositories.UnitOfWorkCore;

/// <summary>
/// Provides validation and dictionary-building utilities for entity data integrity in upsert operations.
/// </summary>
internal static class EntityValidator
{
    /// <summary>
    /// Validates that incoming entities have unique primary keys and throws an exception if duplicates are found.
    /// </summary>
    public static void ValidateIncomingData<TEntity>(List<TEntity> incomingList,
                                                     EntityMetadata<TEntity> metadata,
                                                     LobName lobName,
                                                     string entityName,
                                                     ILogger logger)
        where TEntity : class
    {
        List<IGrouping<object, TEntity>> duplicates = incomingList
                                                     .GroupBy(metadata.GetCompositeKey)
                                                     .Where(g => g.Count() > 1)
                                                     .ToList();

        if (duplicates.Count == 0) return;

        LogDuplicateKeys(duplicates,
                         metadata,
                         lobName,
                         entityName,
                         incomingList.Count,
                         logger);

        throw new
            EntityOperationException($"Duplicate keys detected in incoming data for entity '{entityName}'. Found {duplicates.Count} duplicate key group(s).",
                                     entityName);
    }

    /// <summary>
    /// Builds a dictionary mapping primary keys to database entities for efficient lookup during merge operations.
    /// </summary>
    public static Dictionary<object, TEntity> BuildEntityDictionary<TEntity>(
        List<TEntity> dbEntities,
        EntityMetadata<TEntity> metadata)
        where TEntity : class
    {
        return dbEntities
              .Select(e => (Key: metadata.GetCompositeKey(e), Entity: e))
              .ToDictionary(x => x.Key, x => x.Entity);
    }

    #region ========== *** Private Methods *** ==========

    private static void LogDuplicateKeys<TEntity>(List<IGrouping<object, TEntity>> duplicates,
                                                  EntityMetadata<TEntity> metadata,
                                                  LobName lobName,
                                                  string entityName,
                                                  int totalCount,
                                                  ILogger logger)
        where TEntity : class
    {
        logger.LogError(LobLogTemplates.LobEntity
                        + "Duplicate incoming keys detected | DuplicateGroups={DuplicateCount} TotalIncoming={TotalIncoming}",
                        lobName,
                        entityName,
                        duplicates.Count,
                        totalCount);

        foreach (IGrouping<object, TEntity> duplicateGroup in duplicates.Take(10))
        {
            TEntity firstEntity = duplicateGroup.First();
            string keyString = metadata.GetKeyString(firstEntity);

            logger.LogError(LobLogTemplates.LobEntity + "Duplicate key | Key={KeyString} Occurrences={Count}",
                            lobName,
                            entityName,
                            keyString,
                            duplicateGroup.Count());
        }

        if (duplicates.Count > 10)
        {
            logger.LogError(LobLogTemplates.LobEntity
                            + "More duplicate groups omitted from logs | Omitted={OmittedCount}",
                            lobName,
                            entityName,
                            duplicates.Count - 10);
        }
    }

    #endregion
}
