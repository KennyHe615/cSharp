using System.Linq.Expressions;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;


namespace Infrastructure.Persistence.Repositories.UnitOfWorkCore;

/// <summary>
/// Builds optimized Entity Framework Core queries to fetch entities matching incoming primary keys.
/// </summary>
/// <remarks>
/// <para>
/// This class generates type-safe, database-translatable LINQ expressions to efficiently query entities
/// by their primary keys. It handles both single-key and composite-key scenarios with automatic batching
/// for large datasets to prevent SQL query performance degradation.
/// </para>
/// <para>
/// Performance Optimization: Composite key queries with more than 500 entities are automatically
/// split into batches to avoid excessively large SQL WHERE clauses that can cause poor query plans or parameter limits.
/// </para>
/// <para>
/// Query Translation: All queries are built using strongly-typed expressions that EF Core can
/// translate to efficient SQL IN clauses (single keys) or OR-of-ANDs predicates (composite keys).
/// </para>
/// </remarks>
internal static class EntityQueryBuilder
{
    private const int MaxBatchSizeForCompositeKeys = 500;

    /// <summary>
    /// Fetches entities from the database that match the primary keys of the incoming entity list.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to query.</typeparam>
    /// <param name="dbContext">The Entity Framework Core database context.</param>
    /// <param name="incomingList">The list of incoming entities whose keys will be used to filter the database query.</param>
    /// <param name="metadata">The entity metadata containing primary key information.</param>
    /// <param name="ct">Cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A list of entities fetched from the database that have primary keys matching those in the incoming list.
    /// Returns an empty list if the incoming list is empty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Single Key Strategy: Uses SQL IN clause for optimal performance.
    /// SQL: WHERE KeyProperty IN (value1, value2, ..., valueN)
    /// </para>
    /// <para>
    /// Composite Key Strategy (≤500 items): Builds OR-of-ANDs predicate.
    /// SQL: WHERE (Key1 = v1 AND Key2 = v2) OR (Key1 = v3 AND Key2 = v4) ...
    /// </para>
    /// <para>
    /// Composite Key Strategy (&gt;500 items): Automatically batches into chunks of 500 to prevent
    /// SQL parameter overflow and maintains reasonable query complexity.
    /// </para>
    /// </remarks>
    /// <exception cref="EntityOperationException">
    /// Thrown when primary key properties are missing PropertyInfo or contain null values.
    /// </exception>
    public static async Task<List<TEntity>> FetchMatchingEntitiesAsync<TEntity>(
        DbContext dbContext,
        List<TEntity> incomingList,
        EntityMetadata<TEntity> metadata,
        CancellationToken ct) where TEntity : class
    {
        if (incomingList.Count == 0) return [];

        IQueryable<TEntity> query = dbContext.Set<TEntity>();

        if (metadata.PrimaryKey.Properties.Count == 1)
        {
            query = ApplySingleKeyFilter(query, incomingList, metadata);

            return await query.ToListAsync(ct).ConfigureAwait(false);
        }

        if (incomingList.Count > MaxBatchSizeForCompositeKeys)
        {
            return await FetchInBatches(query, incomingList, metadata, ct).ConfigureAwait(false);
        }

        query = ApplyCompositeKeyFilter(query, incomingList, metadata);

        return await query.ToListAsync(ct).ConfigureAwait(false);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Builds a WHERE IN clause filter for entities with single-property primary keys.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to filter.</typeparam>
    /// <param name="query">The base queryable to filter.</param>
    /// <param name="incomingList">The incoming entities whose key values will form the IN list.</param>
    /// <param name="metadata">The entity metadata containing primary key information.</param>
    /// <returns>A filtered queryable with the IN clause applied.</returns>
    /// <remarks>
    /// Generates an expression tree equivalent to: entity => keyValues.Contains(entity.KeyProperty)
    /// <para>
    /// The method creates a strongly-typed list (e.g., List&lt;Guid&gt;, List&lt;int&gt;) rather than List&lt;object&gt;
    /// to ensure EF Core can properly translate the query to SQL and maintain type safety.
    /// </para>
    /// <para>
    /// Null key values are validated and rejected before query construction to prevent invalid expressions.
    /// </para>
    /// </remarks>
    /// <exception cref="EntityOperationException">
    /// Thrown when the key property lacks PropertyInfo or contains null values.
    /// </exception>
    private static IQueryable<TEntity> ApplySingleKeyFilter<TEntity>(IQueryable<TEntity> query,
                                                                     List<TEntity> incomingList,
                                                                     EntityMetadata<TEntity> metadata)
        where TEntity : class
    {
        IProperty keyProperty = metadata.PrimaryKey.Properties[0];
        PropertyInfo propertyInfo = keyProperty.PropertyInfo ??
                                    throw new EntityOperationException(
                                        $"Property '{keyProperty.Name}' does not have PropertyInfo.",
                                        typeof(TEntity).Name);

        Type keyType = keyProperty.ClrType;

        object?[] keyValues = incomingList.Select(e => propertyInfo.GetValue(e)).Distinct().ToArray();

        ValidateKeyValues(keyValues, keyProperty.Name, typeof(TEntity).Name);

        object typedList = CreateTypedListOptimized(keyType, keyValues);

        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "e");
        MemberExpression property = Expression.Property(parameter, propertyInfo);
        ConstantExpression valuesConstant = Expression.Constant(typedList);

        MethodCallExpression containsCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [keyType],
            valuesConstant,
            property);

        Expression<Func<TEntity, bool>> lambda = Expression.Lambda<Func<TEntity, bool>>(containsCall, parameter);

        return query.Where(lambda);
    }

    /// <summary>
    /// Fetches entities in batches for large composite key datasets to maintain query performance.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to fetch.</typeparam>
    /// <param name="baseQuery">The base queryable to filter.</param>
    /// <param name="incomingList">The complete list of incoming entities to process.</param>
    /// <param name="metadata">The entity metadata containing primary key information.</param>
    /// <param name="ct">Cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A deduplicated list of all entities fetched across all batches. Entities appearing in multiple
    /// batch results are included only once based on their composite key.
    /// </returns>
    /// <remarks>
    /// Processes the incoming list in chunks of 500 items per batch. Each batch generates a separate
    /// database query, and results are merged with deduplication to handle potential overlaps.
    /// <para>
    /// This approach prevents SQL parameter overflow, reduces query compilation overhead,
    /// and maintains predictable query execution plans.
    /// </para>
    /// </remarks>
    private static async Task<List<TEntity>> FetchInBatches<TEntity>(IQueryable<TEntity> baseQuery,
                                                                     List<TEntity> incomingList,
                                                                     EntityMetadata<TEntity> metadata,
                                                                     CancellationToken ct) where TEntity : class
    {
        List<TEntity> allResults = [];
        HashSet<object> fetchedKeys = [];

        for (int i = 0; i < incomingList.Count; i += MaxBatchSizeForCompositeKeys)
        {
            List<TEntity> batch = incomingList.Skip(i).Take(MaxBatchSizeForCompositeKeys).ToList();

            IQueryable<TEntity> batchQuery = ApplyCompositeKeyFilter(baseQuery, batch, metadata);
            List<TEntity> batchResults = await batchQuery.ToListAsync(ct).ConfigureAwait(false);

            allResults.AddRange(from entity in batchResults
                                let key = metadata.GetCompositeKey(entity)
                                where fetchedKeys.Add(key)
                                select entity);
        }

        return allResults;
    }

    /// <summary>
    /// Builds a composite WHERE clause filter using OR-of-ANDs logic for multi-property primary keys.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to filter.</typeparam>
    /// <param name="query">The base queryable to filter.</param>
    /// <param name="incomingList">The incoming entities whose key values will form the filter criteria.</param>
    /// <param name="metadata">The entity metadata containing composite key information.</param>
    /// <returns>A filtered queryable with the composite key predicate applied.</returns>
    /// <remarks>
    /// Generates an expression tree for composite keys with the pattern:
    /// (Key1 == val1a AND Key2 == val2a) OR (Key1 == val1b AND Key2 == val2b) OR ...
    /// <para>
    /// Each incoming entity contributes one AND clause containing all its key property comparisons,
    /// and these clauses are combined with OR operators.
    /// </para>
    /// <para>
    /// Null key values are validated inline and cause immediate exception to prevent invalid SQL generation.
    /// </para>
    /// </remarks>
    /// <exception cref="EntityOperationException">
    /// Thrown when any key property lacks PropertyInfo or any key value is null.
    /// </exception>
    private static IQueryable<TEntity> ApplyCompositeKeyFilter<TEntity>(IQueryable<TEntity> query,
                                                                        List<TEntity> incomingList,
                                                                        EntityMetadata<TEntity> metadata)
        where TEntity : class
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "e");

        Expression? combinedExpression = incomingList.Select(incoming =>
                                                             {
                                                                 Expression? keyExpression = null;

                                                                 foreach (IProperty keyProperty in metadata.PrimaryKey
                                                                              .Properties)
                                                                 {
                                                                     PropertyInfo propertyInfo =
                                                                         keyProperty.PropertyInfo ??
                                                                         throw new EntityOperationException(
                                                                             $"Property '{keyProperty.Name}' does not have PropertyInfo.",
                                                                             typeof(TEntity).Name);

                                                                     object? keyValue = propertyInfo.GetValue(incoming);

                                                                     if (keyValue == null)
                                                                     {
                                                                         throw new EntityOperationException(
                                                                             $"Primary key property '{keyProperty.Name}' is null in incoming entity. Primary keys cannot be null.",
                                                                             typeof(TEntity).Name);
                                                                     }

                                                                     MemberExpression property =
                                                                         Expression.Property(parameter, propertyInfo);
                                                                     ConstantExpression constant =
                                                                         Expression.Constant(
                                                                             keyValue,
                                                                             keyProperty.ClrType);
                                                                     BinaryExpression equals =
                                                                         Expression.Equal(property, constant);

                                                                     keyExpression = keyExpression == null
                                                                         ? equals
                                                                         : Expression.AndAlso(keyExpression, equals);
                                                                 }

                                                                 return keyExpression;
                                                             })
                                                     .Where(expr => expr != null)
                                                     .Aggregate<Expression?, Expression?>(null,
                                                         (current, keyExpression) =>
                                                             current == null
                                                                 ? keyExpression
                                                                 : Expression.OrElse(current, keyExpression!));

        if (combinedExpression == null) return query;

        Expression<Func<TEntity, bool>> lambda = Expression.Lambda<Func<TEntity, bool>>(combinedExpression, parameter);

        return query.Where(lambda);
    }

    /// <summary>
    /// Creates a strongly-typed list instance for the specified key type using reflection and IList interface.
    /// </summary>
    /// <param name="keyType">The CLR type of the primary key property (e.g., Guid, int, string).</param>
    /// <param name="keyValues">The array of key values to add to the list.</param>
    /// <returns>
    /// A List&lt;TKeyType&gt; instance populated with the provided key values, returned as an object
    /// for use in expression tree constant values.
    /// </returns>
    /// <remarks>
    /// This method constructs a generic List&lt;T&gt; at runtime where T is the key type.
    /// Using the IList interface for adding items avoids repeated MethodInfo reflection calls,
    /// providing ~10-20x better performance compared to invoking the Add method via reflection for each item.
    /// <para>
    /// The strongly-typed list is critical for EF Core query translation - using List&lt;object&gt; would
    /// cause type mismatches and prevent proper SQL generation.
    /// </para>
    /// </remarks>
    /// <exception cref="EntityOperationException">
    /// Thrown if the List&lt;TKeyType&gt; instance cannot be created via Activator.CreateInstance.
    /// </exception>
    private static object CreateTypedListOptimized(Type keyType, object?[] keyValues)
    {
        Type listType = typeof(List<>).MakeGenericType(keyType);

        if (Activator.CreateInstance(listType) is not System.Collections.IList typedList)
        {
            throw new EntityOperationException($"Failed to create List<{keyType.Name}>.");
        }

        foreach (object? keyValue in keyValues)
        {
            typedList.Add(keyValue);
        }

        return typedList;
    }

    /// <summary>
    /// Validates that all key values in the array are non-null, as primary keys cannot be null.
    /// </summary>
    /// <param name="keyValues">The array of key values to validate.</param>
    /// <param name="propertyName">The name of the key property being validated (for error messages).</param>
    /// <param name="entityName">The name of the entity type being validated (for error messages).</param>
    /// <remarks>
    /// Primary keys must be non-null to ensure referential integrity and proper query translation.
    /// Null keys would cause expression tree construction errors or invalid SQL WHERE clauses.
    /// </remarks>
    /// <exception cref="EntityOperationException">
    /// Thrown when any value in the keyValues array is null.
    /// </exception>
    private static void ValidateKeyValues(object?[] keyValues, string propertyName, string entityName)
    {
        if (keyValues.Any(v => v == null))
        {
            throw new EntityOperationException(
                $"Primary key property '{propertyName}' contains null values in incoming data for entity '{entityName}'. " +
                "Primary keys cannot be null.",
                entityName);
        }
    }

    #endregion
}
