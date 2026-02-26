using System.Linq.Expressions;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;


namespace Infrastructure.Persistence.Repositories.UnitOfWorkCore;

/// <summary>
/// Builds EF Core queries to fetch entities matching incoming primary keys.
/// Supports single and composite primary keys.
/// </summary>
internal static class EntityQueryBuilder
{
    private const int MaxBatchSizeForCompositeKeys = 500;
    private const int SqlServerMaxParameters = 2100;
    private const int SqlServerSafetyBuffer = 50;

    public static async Task<List<TEntity>> FetchMatchingEntitiesAsync<TEntity>(
        Microsoft.EntityFrameworkCore.DbContext dbContext,
        List<TEntity> incomingList,
        EntityMetadata<TEntity> metadata,
        CancellationToken ct)
        where TEntity : class
    {
        if (incomingList.Count == 0) return [];

        List<TEntity> distinctIncoming = incomingList.GroupBy(metadata.GetCompositeKey).Select(g => g.First()).ToList();
        IQueryable<TEntity> query = dbContext.Set<TEntity>();

        if (metadata.PrimaryKey.Properties.Count == 1)
        {
            return await ApplySingleKeyFilter(query, distinctIncoming, metadata).ToListAsync(ct).ConfigureAwait(false);
        }

        if (distinctIncoming.Count > MaxBatchSizeForCompositeKeys)
        {
            return await FetchCompositeInBatchesAsync(query,
                                                      distinctIncoming,
                                                      metadata,
                                                      ct)
               .ConfigureAwait(false);
        }

        return await ApplyCompositeKeyFilter(query, distinctIncoming, metadata).ToListAsync(ct).ConfigureAwait(false);
    }

    #region ========== *** Private Methods *** ==========

    private static IQueryable<TEntity> ApplySingleKeyFilter<TEntity>(IQueryable<TEntity> query,
                                                                     List<TEntity> incomingList,
                                                                     EntityMetadata<TEntity> metadata)
        where TEntity : class
    {
        IProperty keyProperty = metadata.PrimaryKey.Properties[0];
        PropertyInfo keyPropertyInfo = GetRequiredPropertyInfo<TEntity>(keyProperty);

        object?[] normalizedKeyValues = incomingList
                                       .Select(metadata.GetCompositeKey)// already normalized in EntityMetadata
                                       .Distinct()
                                       .ToArray();

        ValidateKeyValues(normalizedKeyValues, keyProperty.Name, typeof(TEntity).Name);

        object typedList = CreateTypedList(keyProperty.ClrType, normalizedKeyValues);

        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "e");
        MemberExpression keyAccess = Expression.Property(parameter, keyPropertyInfo);
        ConstantExpression valuesConstant = Expression.Constant(typedList);

        MethodCallExpression containsCall =
            Expression.Call(typeof(Enumerable),
                            nameof(Enumerable.Contains),
                            [keyProperty.ClrType],
                            valuesConstant,
                            keyAccess);

        Expression<Func<TEntity, bool>> lambda = Expression.Lambda<Func<TEntity, bool>>(containsCall, parameter);

        return query.Where(lambda);
    }

    private static async Task<List<TEntity>> FetchCompositeInBatchesAsync<TEntity>(
        IQueryable<TEntity> baseQuery,
        List<TEntity> distinctIncoming,
        EntityMetadata<TEntity> metadata,
        CancellationToken ct)
        where TEntity : class
    {
        List<TEntity> allResults = [];
        HashSet<object> fetchedKeys = [];

        int batchSize = GetEffectiveCompositeBatchSize(metadata);

        for (int i = 0; i < distinctIncoming.Count; i += batchSize)
        {
            List<TEntity> batch = distinctIncoming.Skip(i).Take(batchSize).ToList();

            List<TEntity> batchResults = await ApplyCompositeKeyFilter(baseQuery, batch, metadata)
                                              .ToListAsync(ct)
                                              .ConfigureAwait(false);

            allResults.AddRange(from entity in batchResults
                                let key = metadata.GetCompositeKey(entity)
                                where fetchedKeys.Add(key)
                                select entity);
        }

        return allResults;
    }

    private static IQueryable<TEntity> ApplyCompositeKeyFilter<TEntity>(IQueryable<TEntity> query,
                                                                        List<TEntity> incomingList,
                                                                        EntityMetadata<TEntity> metadata)
        where TEntity : class
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "e");

        Expression? disjunction =
            incomingList
               .Select(incoming => BuildCompositeAndExpression(parameter, incoming, metadata))
               .Aggregate<Expression, Expression?>(null,
                                                   (current, conjunction) =>
                                                       current is null
                                                           ? conjunction
                                                           : Expression.OrElse(current, conjunction));

        if (disjunction is null) return query;

        Expression<Func<TEntity, bool>> lambda = Expression.Lambda<Func<TEntity, bool>>(disjunction, parameter);

        return query.Where(lambda);
    }

    private static Expression BuildCompositeAndExpression<TEntity>(ParameterExpression parameter,
                                                                   TEntity incoming,
                                                                   EntityMetadata<TEntity> metadata)
        where TEntity : class
    {
        Expression? conjunction = (from keyProperty in metadata.PrimaryKey.Properties
                                   let keyPropertyInfo = GetRequiredPropertyInfo<TEntity>(keyProperty)
                                   let keyValue = GetRequiredKeyValue(incoming,
                                                                      keyPropertyInfo,
                                                                      keyProperty.Name,
                                                                      typeof(TEntity).Name)
                                   let keyAccess = Expression.Property(parameter, keyPropertyInfo)
                                   let constant = CreateTypedConstant(keyValue, keyProperty.ClrType)
                                   select Expression.Equal(keyAccess, constant))
           .Aggregate<BinaryExpression, Expression?>(null,
                                                     (current, equals) =>
                                                         current is null
                                                             ? equals
                                                             : Expression.AndAlso(current, equals));

        return conjunction
               ?? throw new EntityOperationException("Failed to build composite key predicate expression.",
                                                     typeof(TEntity).Name);
    }

    private static PropertyInfo GetRequiredPropertyInfo<TEntity>(IProperty property)
        where TEntity : class
    {
        // Standard mapped CLR property
        if (property.PropertyInfo is not null) return property.PropertyInfo;

        // If this is field-backed, build a property-like accessor via expression at call site
        throw new
            EntityOperationException($"Primary key '{property.Name}' for '{typeof(TEntity).Name}' is not CLR-property backed. "
                                     + "Current upsert key predicate builder requires CLR property keys only.",
                                     typeof(TEntity).Name);
    }

    private static object GetRequiredKeyValue<TEntity>(TEntity incoming,
                                                       PropertyInfo propertyInfo,
                                                       string propertyName,
                                                       string entityName)
        where TEntity : class
    {
        object? value = propertyInfo.GetValue(incoming);

        if (value is null)
        {
            throw new EntityOperationException($"Primary key property '{propertyName}' is null in incoming entity.",
                                               entityName);
        }

        return value;
    }

    private static ConstantExpression CreateTypedConstant(object value, Type targetType)
    {
        Type? nullableUnderlying = Nullable.GetUnderlyingType(targetType);

        if (nullableUnderlying is null) return Expression.Constant(value, targetType);

        object boxedNullable = Activator.CreateInstance(targetType, value)!;

        return Expression.Constant(boxedNullable, targetType);
    }

    private static object CreateTypedList(Type elementType, object?[] values)
    {
        Type listType = typeof(List<>).MakeGenericType(elementType);

        if (Activator.CreateInstance(listType) is not System.Collections.IList typedList)
        {
            throw new EntityOperationException($"Failed to create List<{elementType.Name}>.");
        }

        foreach (object? value in values)
        {
            typedList.Add(value);
        }

        return typedList;
    }

    private static void ValidateKeyValues(object?[] keyValues, string propertyName, string entityName)
    {
        if (keyValues.Any(keyValue => keyValue is null || keyValue == DBNull.Value))
        {
            throw new
                EntityOperationException($"Primary key property '{propertyName}' contains null/invalid values in incoming data for '{entityName}'.",
                                         entityName);
        }
    }

    private static int GetEffectiveCompositeBatchSize<TEntity>(EntityMetadata<TEntity> metadata)
        where TEntity : class
    {
        int keyCount = metadata.PrimaryKey.Properties.Count;
        int safeByParams = Math.Max(1, (SqlServerMaxParameters - SqlServerSafetyBuffer) / keyCount);

        return Math.Min(MaxBatchSizeForCompositeKeys, safeByParams);
    }

    #endregion
}
