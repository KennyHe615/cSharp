using System.Reflection;

using Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;


namespace Infrastructure.Persistence.Repositories.UnitOfWorkCore;

/// <summary>
/// Handles entity upsert operations and property-level synchronization.
/// </summary>
internal static class EntityUpdateHandler
{
    public static UpsertResult ProcessUpsertOperations<TEntity>(Microsoft.EntityFrameworkCore.DbContext dbContext,
                                                                List<TEntity> incomingList,
                                                                Dictionary<object, TEntity> dbById,
                                                                EntityMetadata<TEntity> metadata)
                    where TEntity : class
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
                dbContext.Set<TEntity>()
                         .Add(incoming);

                addedCount++;
            }
        }

        return new UpsertResult(incomingKeys, addedCount, updatedCount);
    }

    public static void ProcessMissingEntities<TEntity>(List<TEntity> dbEntities,
                                                       HashSet<object> incomingKeys,
                                                       EntityMetadata<TEntity> metadata,
                                                       Action<TEntity> onMissingFromIncoming)
                    where TEntity : class
    {
        foreach (TEntity dbEntity in dbEntities.Where(e => !incomingKeys.Contains(metadata.GetCompositeKey(e))))
        {
            onMissingFromIncoming(dbEntity);
        }
    }

    #region ========== *** Private Methods *** ==========

    private static void UpdateEntity<TEntity>(Microsoft.EntityFrameworkCore.DbContext dbContext,
                                              TEntity existing,
                                              TEntity incoming)
                    where TEntity : class
    {
        EntityEntry<TEntity> existingEntry = dbContext.Entry(existing);

        foreach (IProperty prop in existingEntry.Metadata.GetProperties())
        {
            if (ShouldSkipProperty(prop)) continue;

            PropertyInfo? propertyInfo = prop.PropertyInfo;

            if (propertyInfo is null) continue;

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
    /// Determines whether a mapped property should be excluded from manual upsert copying.
    /// </summary>
    /// <param name="property">The EF Core property metadata to evaluate.</param>
    /// <returns><c>true</c> when the property is framework-managed, audit-managed, or otherwise not manually copied.</returns>
    private static bool ShouldSkipProperty(IProperty property)
    {
        if (property.IsPrimaryKey()) return true;

        // Keep audit columns out of manual copy. Interceptor owns them.
        if (property.Name is nameof(Audit.AppCreatedAtEastern) or nameof(Audit.AppUpdatedAtEastern)) return true;

        return property.ValueGenerated is ValueGenerated.OnAdd or ValueGenerated.OnAddOrUpdate
               || property.IsConcurrencyToken;
    }

    #endregion
}
