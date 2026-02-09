using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;


namespace Infrastructure.Persistence.Repositories.UnitOfWorkCore;

/// <summary>
/// Handles entity upsert (update or insert) operations and property value synchronization for the UnitOfWork pattern.
/// </summary>
/// <remarks>
/// This class manages the merge logic between incoming entities and existing database entities.
/// It determines whether each entity should be added or updated, performs property-level synchronization,
/// and handles special cases like enum string converter normalization.
/// <para>
/// The handler respects Entity Framework Core's change tracking rules and skips immutable properties
/// such as primary keys, database-generated values, and concurrency tokens.
/// </para>
/// </remarks>
internal static class EntityUpdateHandler
{
    /// <summary>
    /// Processes a list of incoming entities by either adding new ones or updating existing ones based on primary key matching.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being processed.</typeparam>
    /// <param name="dbContext">The Entity Framework Core database context.</param>
    /// <param name="incomingList">The list of incoming entities to upsert.</param>
    /// <param name="dbById">A dictionary mapping composite keys to existing database entities.</param>
    /// <param name="metadata">The entity metadata containing primary key information.</param>
    /// <returns>
    /// An <see cref="UpsertResult"/> containing the set of incoming keys and counts of added and updated entities.
    /// </returns>
    /// <remarks>
    /// For each incoming entity:
    /// <list type="bullet">
    /// <item>If the primary key exists in dbById, the existing entity is updated with incoming values</item>
    /// <item>If the primary key is new, the incoming entity is added to the context for insertion</item>
    /// </list>
    /// The method does not call SaveChanges; the caller is responsible for persisting changes.
    /// </remarks>
    public static UpsertResult ProcessUpsertOperations<TEntity>(DbContext dbContext,
                                                                List<TEntity> incomingList,
                                                                Dictionary<object, TEntity> dbById,
                                                                EntityMetadata<TEntity> metadata) where TEntity : class
    {
        HashSet<object> incomingKeys = [];
        int addedCount = 0;
        int updatedCount = 0;

        foreach (TEntity incoming in incomingList)
        {
            object key = metadata.GetCompositeKey(incoming);
            incomingKeys.Add(key);

            if (dbById.TryGetValue(key, out TEntity? existing))
            {
                UpdateEntity(dbContext, existing, incoming);

                updatedCount++;
            }
            else
            {
                dbContext.Set<TEntity>().Add(incoming);

                addedCount++;
            }
        }

        return new UpsertResult(incomingKeys, addedCount, updatedCount);
    }

    /// <summary>
    /// Invokes a callback for each database entity that was fetched but is not present in the incoming list.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being processed.</typeparam>
    /// <param name="dbEntities">The list of entities fetched from the database.</param>
    /// <param name="incomingKeys">The set of primary keys from the incoming entity list.</param>
    /// <param name="metadata">The entity metadata containing primary key information.</param>
    /// <param name="onMissingFromIncoming">
    /// The callback action to invoke for each entity that exists in the database but not in the incoming list.
    /// Typically used for soft-delete or inactivation scenarios.
    /// </param>
    /// <remarks>
    /// This method enables synchronization scenarios where entities that no longer appear in the source data
    /// need to be marked as inactive or deleted. The callback receives each missing entity and can modify it
    /// (e.g., set IsActive = false). Changes are tracked by Entity Framework and persisted on SaveChanges.
    /// <para>
    /// Only entities matching the incoming key set are considered; this is not a full table scan.
    /// </para>
    /// </remarks>
    public static void ProcessMissingEntities<TEntity>(List<TEntity> dbEntities,
                                                       HashSet<object> incomingKeys,
                                                       EntityMetadata<TEntity> metadata,
                                                       Action<TEntity> onMissingFromIncoming) where TEntity : class
    {
        foreach (TEntity dbEntity in dbEntities.Where(e => !incomingKeys.Contains(metadata.GetCompositeKey(e))))
        {
            onMissingFromIncoming(dbEntity);
        }
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Updates an existing tracked entity with values from an incoming entity, property by property.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being updated.</typeparam>
    /// <param name="dbContext">The Entity Framework Core database context.</param>
    /// <param name="existing">The existing entity tracked by the context (fetched from the database).</param>
    /// <param name="incoming">The incoming entity containing new values.</param>
    /// <remarks>
    /// The method iterates through all non-skipped properties and copies values from the incoming entity
    /// to the existing entity. Properties are skipped if they are:
    /// <list type="bullet">
    /// <item>Primary key properties</item>
    /// <item>Database-generated properties (identity, computed columns)</item>
    /// <item>Concurrency tokens (e.g., RowVersion)</item>
    /// </list>
    /// <para>
    /// Special handling for string value converters: If a property uses a value converter that stores values
    /// as strings in the database (e.g., enum to uppercase snake_case), the method compares provider-side values
    /// to detect normalization differences and forces the Modified flag even when CLR values appear equal.
    /// This ensures database values are updated to the normalized format.
    /// </para>
    /// <para>
    /// Example: SystemPresence enum with values ON_QUEUE vs on_queue. Both map to the same enum value,
    /// but the database should store the normalized form (ON_QUEUE). The explicit IsModified = true ensures
    /// the UPDATE statement is generated.
    /// </para>
    /// </remarks>
    private static void UpdateEntity<TEntity>(DbContext dbContext, TEntity existing, TEntity incoming)
        where TEntity : class
    {
        EntityEntry<TEntity> existingEntry = dbContext.Entry(existing);

        foreach (IProperty prop in existingEntry.Metadata.GetProperties())
        {
            if (ShouldSkipProperty(prop)) continue;

            PropertyInfo? propertyInfo = prop.PropertyInfo;

            if (propertyInfo == null) continue;

            object? newValue = propertyInfo.GetValue(incoming);
            object? currentValue = propertyInfo.GetValue(existing);

            if (Equals(currentValue, newValue)) continue;

            PropertyEntry propertyEntry = existingEntry.Property(prop.Name);
            propertyEntry.CurrentValue = newValue;

            ValueConverter? converter = prop.GetValueConverter();

            if (converter?.ProviderClrType != typeof(string)) continue;

            object? existingProvider = converter.ConvertToProvider(currentValue);
            object? incomingProvider = converter.ConvertToProvider(newValue);

            if (!Equals(existingProvider, incomingProvider))
            {
                propertyEntry.IsModified = true;
            }
        }
    }

    /// <summary>
    /// Determines whether a property should be skipped during entity updates.
    /// </summary>
    /// <param name="property">The Entity Framework Core property metadata.</param>
    /// <returns>
    /// <c>true</c> if the property should not be updated; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Properties are skipped if:
    /// <list type="bullet">
    /// <item>They are part of the primary key (EF Core forbids changing key values on tracked entities)</item>
    /// <item>They are generated on add (e.g., IDENTITY columns, default values)</item>
    /// <item>They are generated on add or update (e.g., computed columns, timestamps)</item>
    /// <item>They are concurrency tokens (e.g., RowVersion, managed by EF Core)</item>
    /// </list>
    /// These properties are managed by the database or Entity Framework and should not be manually set.
    /// </remarks>
    private static bool ShouldSkipProperty(IProperty property)
    {
        if (property.IsPrimaryKey()) return true;

        return property.ValueGenerated is ValueGenerated.OnAdd or ValueGenerated.OnAddOrUpdate ||
               property.IsConcurrencyToken;
    }

    #endregion
}
