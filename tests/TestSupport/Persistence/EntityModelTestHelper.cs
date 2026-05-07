using Infrastructure.Persistence.DbContext;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;


namespace tests.TestSupport.Persistence;

/// <summary>
/// Shared helpers for inspecting Entity Framework model metadata in persistence configuration tests.
/// </summary>
public static class EntityModelTestHelper
{
    /// <summary>
    /// Gets the design-time entity model metadata for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">Entity CLR type to resolve from the EF model.</typeparam>
    /// <param name="dbContext">Application database context used to access the design-time model.</param>
    /// <returns>The EF entity type metadata for <typeparamref name="TEntity"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity type is not present in the EF model.</exception>
    public static IEntityType GetEntityType<TEntity>(AppDbContext dbContext)
    {
        return dbContext.GetService<IDesignTimeModel>()
                        .Model.FindEntityType(typeof(TEntity))
               ?? throw new InvalidOperationException($"{typeof(TEntity).Name} model was not found.");
    }

    /// <summary>
    /// Finds one index whose property list exactly matches the supplied property names.
    /// </summary>
    /// <param name="entityType">Entity type metadata to inspect.</param>
    /// <param name="propertyNames">Expected index property names in order.</param>
    /// <returns>The matching EF index metadata.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no matching index exists or multiple indexes match.</exception>
    public static IIndex FindIndex(IEntityType entityType, params string[] propertyNames)
    {
        return entityType.GetIndexes()
                         .Single(index => index.Properties.Select(x => x.Name)
                                               .SequenceEqual(propertyNames));
    }

    /// <summary>
    /// Determines whether an index exists whose property list exactly matches the supplied property names.
    /// </summary>
    /// <param name="entityType">Entity type metadata to inspect.</param>
    /// <param name="propertyNames">Expected index property names in order.</param>
    /// <returns><c>true</c> when a matching index exists; otherwise <c>false</c>.</returns>
    public static bool HasIndex(IEntityType entityType, params string[] propertyNames)
    {
        return entityType.GetIndexes()
                         .Any(index => index.Properties.Select(x => x.Name)
                                            .SequenceEqual(propertyNames));
    }
}
