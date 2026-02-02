using Infrastructure.Exceptions;


namespace Infrastructure.Persistence;

/// <summary>
/// Base exception for all persistence-related errors in the Infrastructure.Persistence layer.
/// </summary>
public abstract class PersistenceException : InfrastructureException
{
    protected PersistenceException()
    {
    }

    protected PersistenceException(string message) : base(message)
    {
    }

    protected PersistenceException(string message, Exception? inner) : base(message, inner)
    {
    }
}

/// <summary>
/// Thrown when database connection or configuration fails.
/// </summary>
public sealed class DbContextConfigurationException : PersistenceException
{
    public DbContextConfigurationException(string message) : base(message)
    {
    }

    public DbContextConfigurationException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// Thrown when SaveChanges operation fails due to concurrency conflicts.
/// </summary>
public sealed class DbConcurrencyException(string message,
                                           Exception inner) : PersistenceException(message, inner);

/// <summary>
/// Thrown when a database constraint violation occurs (e.g., unique key, foreign key).
/// </summary>
public sealed class DbConstraintViolationException(string message,
                                                   Exception inner,
                                                   string? constraintName = null) : PersistenceException(message, inner)
{
    public string? ConstraintName { get; } = constraintName;
}

/// <summary>
/// Thrown when an entity operation fails (e.g., entity not found, invalid primary key).
/// </summary>
public sealed class EntityOperationException : PersistenceException
{
    public string? EntityName { get; }

    public EntityOperationException(string message, string? entityName = null) : base(message)
    {
        EntityName = entityName;
    }

    public EntityOperationException(string message, Exception inner, string? entityName = null) : base(message, inner)
    {
        EntityName = entityName;
    }
}

/// <summary>
/// Thrown when entity mapping fails during persistence operations.
/// </summary>
public sealed class EntityMappingException(string message,
                                           Exception inner,
                                           Type? sourceType = null,
                                           Type? destinationType = null) : PersistenceException(message, inner)
{
    public Type? SourceType { get; } = sourceType;

    public Type? DestinationType { get; } = destinationType;
}
