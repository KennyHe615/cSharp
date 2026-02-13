using Microsoft.EntityFrameworkCore.Metadata;

using Shared.Time;


namespace Infrastructure.Persistence.Repositories.UnitOfWorkCore;

/// <summary>
/// Provides metadata operations and key extraction for entity types in the UnitOfWork pattern.
/// </summary>
/// <typeparam name="TEntity">The entity type that this metadata instance represents.</typeparam>
/// <remarks>
/// This class encapsulates Entity Framework Core metadata and provides utilities for:
/// <list type="bullet">
/// <item>Extracting primary key values (single or composite) from entity instances</item>
/// <item>Normalizing key values for consistent comparison (e.g., rounding DateTimeOffset to seconds)</item>
/// <item>Generating human-readable key strings for logging and diagnostics</item>
/// </list>
/// Key normalization ensures that entities with semantically identical keys are treated as equal,
/// even if their raw values differ slightly (e.g., microsecond precision in timestamps).
/// </remarks>
internal sealed class EntityMetadata<TEntity>(IEntityType entityType,
                                              IKey primaryKey) where TEntity : class
{
    /// <summary>
    /// Gets the Entity Framework Core primary key metadata for the entity type.
    /// </summary>
    /// <value>
    /// An <see cref="IKey"/> instance representing the primary key definition,
    /// which may contain one or more properties for composite keys.
    /// </value>
    public IKey PrimaryKey => primaryKey;

    /// <summary>
    /// Extracts and normalizes the primary key value(s) from an entity instance.
    /// </summary>
    /// <param name="entity">The entity instance from which to extract the key.</param>
    /// <returns>
    /// For single-key entities, returns the normalized key value directly.
    /// For composite-key entities, returns a <see cref="CompositeKey"/> instance containing all normalized key values.
    /// </returns>
    /// <remarks>
    /// Key values are normalized to ensure consistent comparison:
    /// <list type="bullet">
    /// <item><see cref="DateTimeOffset"/> values are converted to EST and rounded to the nearest second</item>
    /// <item>Null values are converted to <see cref="DBNull.Value"/></item>
    /// <item>Other types are returned as-is</item>
    /// </list>
    /// The returned value is suitable for use as a dictionary key or in hash-based collections.
    /// </remarks>
    public object GetCompositeKey(TEntity entity)
    {
        if (primaryKey.Properties.Count == 1)
        {
            object? rawValue = GetPropertyValue(entity, primaryKey.Properties[0].Name);

            return NormalizeKeyValue(rawValue);
        }

        object?[] keyValues = new object?[primaryKey.Properties.Count];
        for (int i = 0; i < primaryKey.Properties.Count; i++)
        {
            object? rawValue = GetPropertyValue(entity, primaryKey.Properties[i].Name);
            keyValues[i] = NormalizeKeyValue(rawValue);
        }

        return new CompositeKey(keyValues);
    }

    /// <summary>
    /// Generates a human-readable string representation of the entity's primary key for logging and diagnostics.
    /// </summary>
    /// <param name="entity">The entity instance from which to extract the key.</param>
    /// <returns>
    /// For single-key entities, returns a string in the format "PropertyName=Value".
    /// For composite-key entities, returns a comma-separated string like "Property1=Value1, Property2=Value2".
    /// </returns>
    /// <remarks>
    /// This method is primarily used for error messages, logs, and debugging output.
    /// Key values are normalized before being converted to strings.
    /// </remarks>
    public string GetKeyString(TEntity entity)
    {
        if (primaryKey.Properties.Count == 1)
        {
            object? rawValue = GetPropertyValue(entity, primaryKey.Properties[0].Name);
            object normalized = NormalizeKeyValue(rawValue);

            return $"{primaryKey.Properties[0].Name}={normalized}";
        }

        string[] keyParts = new string[primaryKey.Properties.Count];
        for (int i = 0; i < primaryKey.Properties.Count; i++)
        {
            object? rawValue = GetPropertyValue(entity, primaryKey.Properties[i].Name);
            object normalized = NormalizeKeyValue(rawValue);
            keyParts[i] = $"{primaryKey.Properties[i].Name}={normalized}";
        }

        return string.Join(", ", keyParts);
    }

    /// <summary>
    /// Retrieves the value of a property from an entity instance using Entity Framework Core metadata.
    /// </summary>
    /// <param name="entity">The entity instance from which to read the property.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The property value, or <c>null</c> if the property value is null.</returns>
    /// <remarks>
    /// This method uses EF Core's compiled property getters for optimal performance,
    /// avoiding reflection on every call after the first access.
    /// </remarks>
    private object? GetPropertyValue(TEntity entity, string propertyName)
    {
        return entityType.FindProperty(propertyName)!.GetGetter().GetClrValue(entity);
    }

    /// <summary>
    /// Normalizes a key value to ensure consistent comparison across different representations.
    /// </summary>
    /// <param name="value">The raw key value to normalize.</param>
    /// <returns>
    /// The normalized value. Transformations applied:
    /// <list type="bullet">
    /// <item><see cref="DateTimeOffset"/>: Converted to EST timezone</item>
    /// <item><c>null</c>: Converted to <see cref="DBNull.Value"/></item>
    /// <item>All other types: Returned unchanged</item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// Null handling: <see cref="DBNull.Value"/> is used instead of null to prevent issues
    /// with null keys in collections and to provide a consistent non-null representation.
    /// </para>
    /// </remarks>
    private static object NormalizeKeyValue(object? value)
    {
        return value switch
        {
            DateTimeOffset dto => DateTimeResolver.ConvertToEst(dto),
            null => DBNull.Value,
            _ => value
        };
    }
}
