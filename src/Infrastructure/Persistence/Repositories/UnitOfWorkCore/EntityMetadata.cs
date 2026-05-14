using Infrastructure.Persistence.DbContext;

using Microsoft.EntityFrameworkCore.Metadata;

using SharedKernel.Time;


namespace Infrastructure.Persistence.Repositories.UnitOfWorkCore;

/// <summary>
/// Provides metadata operations and key extraction for entity types in the UnitOfWork pattern.
/// </summary>
/// <typeparam name="TEntity">The entity type that this metadata instance represents.</typeparam>
internal sealed class EntityMetadata<TEntity>(IEntityType entityType,
                                              IKey primaryKey,
                                              IDateTimeProvider dateTimeProvider)
        where TEntity : class
{
    public IKey PrimaryKey => primaryKey;

    /// <summary>
    /// Extracts and normalizes the primary key value(s) from an entity instance.
    /// Returns a scalar for single-key entities and <see cref="CompositeKey"/> for composite keys.
    /// </summary>
    public object GetCompositeKey(TEntity entity)
    {
        if (primaryKey.Properties.Count == 1)
        {
            IProperty property = primaryKey.Properties[0];
            object? rawValue = GetPropertyValue(entity, property.Name);

            return NormalizeKeyValue(rawValue, property);
        }

        object?[] keyValues = new object?[primaryKey.Properties.Count];
        for (int i = 0; i < primaryKey.Properties.Count; i++)
        {
            IProperty property = primaryKey.Properties[i];
            object? rawValue = GetPropertyValue(entity, property.Name);
            keyValues[i] = NormalizeKeyValue(rawValue, property);
        }

        return new CompositeKey(keyValues);
    }

    /// <summary>
    /// Returns a human-readable key string for logging and diagnostics.
    /// </summary>
    public string GetKeyString(TEntity entity)
    {
        if (primaryKey.Properties.Count == 1)
        {
            IProperty property = primaryKey.Properties[0];
            object? rawValue = GetPropertyValue(entity, property.Name);
            object normalized = NormalizeKeyValue(rawValue, property);

            return $"{property.Name}={normalized}";
        }

        string[] keyParts = new string[primaryKey.Properties.Count];
        for (int i = 0; i < primaryKey.Properties.Count; i++)
        {
            IProperty property = primaryKey.Properties[i];
            object? rawValue = GetPropertyValue(entity, property.Name);
            object normalized = NormalizeKeyValue(rawValue, property);
            keyParts[i] = $"{property.Name}={normalized}";
        }

        return string.Join(", ", keyParts);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Retrieves the value of a property from an entity instance using Entity Framework Core metadata.
    /// </summary>
    private object? GetPropertyValue(TEntity entity, string propertyName)
    {
        return entityType.FindProperty(propertyName)!.GetGetter()
                         .GetClrValue(entity);
    }

    private object NormalizeKeyValue(object? value, IProperty property)
    {
        return value switch
               {
                   DateTimeOffset dto when !AppDbContext.IsUtcDateTimeOffsetProperty(property) => dateTimeProvider
                          .ConvertToEst(dto),
                   null => DBNull.Value,
                   _ => value
               };
    }

    #endregion
}
