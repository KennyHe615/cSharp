using Microsoft.Extensions.Logging;


namespace Infrastructure.Persistence.Repositories.UnitOfWorkCore;

/// <summary>
/// Provides validation and dictionary-building utilities for entity data integrity in upsert operations.
/// </summary>
/// <remarks>
/// This class ensures that incoming entity data meets integrity constraints before database operations
/// are performed. It validates that primary keys are unique within a batch and provides efficient
/// lookup structures for merge operations.
/// <para>
/// Validation failures are logged with detailed diagnostic information to aid in troubleshooting
/// data quality issues at the source.
/// </para>
/// </remarks>
internal static class EntityValidator
{
    /// <summary>
    /// Validates that incoming entities have unique primary keys and throws an exception if duplicates are found.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being validated.</typeparam>
    /// <param name="incomingList">The list of incoming entities to validate.</param>
    /// <param name="metadata">The entity metadata containing primary key information.</param>
    /// <param name="entityName">The name of the entity type (used for error messages and logging).</param>
    /// <param name="logger">The logger instance for recording validation errors.</param>
    /// <remarks>
    /// Duplicate primary keys within the same batch indicate a data integrity problem in the source system
    /// or mapping logic. This validation prevents undefined behavior where Entity Framework would only
    /// process one entity per key, silently discarding duplicates.
    /// <para>
    /// When duplicates are detected, detailed error logs are emitted showing the duplicate keys and
    /// occurrence counts. Only the first 10 duplicate key groups are logged to prevent log flooding
    /// with very large batches.
    /// </para>
    /// </remarks>
    /// <exception cref="EntityOperationException">
    /// Thrown when duplicate primary keys are detected. The exception message includes the total count
    /// of duplicate key groups and directs the user to check logs for specific key details.
    /// </exception>
    public static void ValidateIncomingData<TEntity>(List<TEntity> incomingList,
                                                     EntityMetadata<TEntity> metadata,
                                                     string entityName,
                                                     ILogger logger) where TEntity : class
    {
        List<IGrouping<object, TEntity>> duplicates = incomingList
                                                      .GroupBy(metadata.GetCompositeKey)
                                                      .Where(g => g.Count() > 1)
                                                      .ToList();

        if (duplicates.Count == 0) return;

        LogDuplicateKeys(duplicates, metadata, entityName, incomingList.Count, logger);

        throw new EntityOperationException($"Duplicate keys detected in incoming data for entity '{entityName}'. " +
                                           $"Found {duplicates.Count} duplicate key(s). Check logs for details.",
                                           entityName);
    }

    /// <summary>
    /// Builds a dictionary mapping primary keys to database entities for efficient lookup during merge operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being indexed.</typeparam>
    /// <param name="dbEntities">The list of entities fetched from the database.</param>
    /// <param name="metadata">The entity metadata containing primary key information.</param>
    /// <returns>
    /// A dictionary where keys are composite primary key objects (single values or <see cref="CompositeKey"/> instances)
    /// and values are the corresponding entity instances.
    /// </returns>
    /// <remarks>
    /// This dictionary enables O(1) lookup time when determining whether an incoming entity should be
    /// added (new key) or updated (existing key). Without this index, the merge operation would require
    /// O(n*m) comparisons where n is the incoming entity count and m is the database entity count.
    /// <para>
    /// The composite keys returned by <see cref="EntityMetadata{TEntity}.GetCompositeKey"/> implement
    /// structural equality, ensuring correct dictionary behavior for composite keys.
    /// </para>
    /// </remarks>
    public static Dictionary<object, TEntity> BuildEntityDictionary<TEntity>(
        List<TEntity> dbEntities,
        EntityMetadata<TEntity> metadata) where TEntity : class
    {
        return dbEntities.Select(e => (Key: metadata.GetCompositeKey(e), Entity: e))
                         .ToDictionary(x => x.Key, x => x.Entity);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Logs detailed information about duplicate primary keys found in the incoming data.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being validated.</typeparam>
    /// <param name="duplicates">The list of grouped duplicate entities, where each group shares the same primary key.</param>
    /// <param name="metadata">The entity metadata containing primary key information.</param>
    /// <param name="entityName">The name of the entity type (used for log messages).</param>
    /// <param name="totalCount">The total count of incoming entities (used for context in logs).</param>
    /// <param name="logger">The logger instance for recording error messages.</param>
    /// <remarks>
    /// The method logs three types of messages:
    /// <list type="number">
    /// <item>A summary message with the total duplicate count and total incoming record count</item>
    /// <item>Individual error messages for each duplicate key group (up to 10) showing the key and occurrence count</item>
    /// <item>An overflow message if more than 10 duplicate key groups exist</item>
    /// </list>
    /// <para>
    /// Limiting to 10 duplicate groups prevents overwhelming the logging system while still providing
    /// sufficient diagnostic information. Full entity serialization is avoided to prevent exposing
    /// sensitive data in logs and to reduce log volume.
    /// </para>
    /// </remarks>
    private static void LogDuplicateKeys<TEntity>(List<IGrouping<object, TEntity>> duplicates,
                                                  EntityMetadata<TEntity> metadata,
                                                  string entityName,
                                                  int totalCount,
                                                  ILogger logger) where TEntity : class
    {
        logger.LogError(
            "[{EntityName}] Found {DuplicateCount} duplicate key(s) in incoming data (Total: {TotalIncoming} records)",
            entityName,
            duplicates.Count,
            totalCount);

        foreach (IGrouping<object, TEntity> duplicateGroup in duplicates.Take(10))
        {
            TEntity firstEntity = duplicateGroup.First();
            string keyString = metadata.GetKeyString(firstEntity);

            logger.LogError("[{EntityName}] Duplicate Key: [{KeyString}] - Found {Count} occurrences",
                            entityName,
                            keyString,
                            duplicateGroup.Count());
        }

        if (duplicates.Count > 10)
        {
            logger.LogError("[{EntityName}] ... and {More} more duplicate key groups (showing first 10 only)",
                            entityName,
                            duplicates.Count - 10);
        }
    }

    #endregion
}
