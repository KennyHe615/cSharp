using Application.Shared.Context;

using Configuration.Options;

using Infrastructure.Persistence.Entities;
using Infrastructure.Persistence.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

using Shared.Extensions;


namespace Infrastructure.Persistence.FunctionAppDbContext;

/// <summary>
/// The primary database context for the Function App, responsible for managing entity mappings and database connectivity.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
/// <param name="databaseOptions">Global database configuration options.</param>
/// <param name="lobContext">The current Line of Business (LOB) context for dynamic connection string resolution.</param>
/// <param name="auditInterceptor">Interceptor for automatically applying auditing metadata to entities.</param>
/// <remarks>
/// This context supports multi-tenancy (LOB-based) by dynamically resolving connection strings at runtime via <see cref="ILobContext"/>.
/// </remarks>
public class FunctionAppDbContext(DbContextOptions<FunctionAppDbContext> options,
                                  IOptions<DatabaseOptions> databaseOptions,
                                  ILobContext lobContext,
                                  AuditSaveChangesInterceptor auditInterceptor) : DbContext(options)
{
    /// <summary>
    /// Configures the database context options, including connection string resolution and auditing.
    /// </summary>
    /// <param name="optionsBuilder">The builder used to create or modify options for this context.</param>
    /// <exception cref="DbContextConfigurationException">Thrown if the connection string for the current LOB is missing or invalid.</exception>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Dynamic connection string resolution based on current LOB Context
        if (optionsBuilder.IsConfigured) return;

        DatabaseOptions dbOptions = databaseOptions.Value;

        // 1. ILobContext is scoped and initialized by SyncOrchestrator for each run.
        string connectionString = lobContext.DbConnStr;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new DbContextConfigurationException(
                $"Database ConnectionString for LOB '{lobContext.LobName}' is missing.");
        }

        try
        {
            // 2. Use global settings for Retry and Timeout
            optionsBuilder.UseSqlServer(connectionString,
                                        sqlOptions =>
                                        {
                                            sqlOptions.EnableRetryOnFailure(dbOptions.MaxRetryCount);
                                            sqlOptions.CommandTimeout(dbOptions.CommandTimeout);
                                        });
        }
        catch (ArgumentException ex)
        {
            throw new DbContextConfigurationException($"Invalid connection string for LOB '{lobContext.LobName}'.", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new DbContextConfigurationException($"Failed to configure DbContext for LOB '{lobContext.LobName}'.",
                                                      ex);
        }

        if (dbOptions.EnableDetailedErrors) optionsBuilder.EnableDetailedErrors();
        if (dbOptions.EnableSensitiveDataLogging) optionsBuilder.EnableSensitiveDataLogging();

        // Register auditing so it runs for all SaveChanges calls, without repository code.
        optionsBuilder.AddInterceptors(auditInterceptor);
    }

    /// <summary>
    /// Configures the entity models, applying snake_case naming conventions and loading entity configurations from the assembly.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This automatically finds all classes in this assembly that implement IEntityTypeConfiguration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FunctionAppDbContext).Assembly);

        // Apply naming convention for all entities and properties
        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
        {
            // Table name can be null for non-table mapped types; skip those safely.
            string? tableName = entity.GetTableName();
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                entity.SetTableName(tableName.ToSnakeCase());
            }

            // Set column names to snake_case
            foreach (IMutableProperty property in entity.GetProperties())
            {
                // Use the property name directly to generate the column name
                property.SetColumnName(property.Name.ToSnakeCase());
            }

            // Set key and index names to snake_case
            foreach (IMutableKey key in entity.GetKeys())
            {
                string? keyName = key.GetName();
                if (!string.IsNullOrWhiteSpace(keyName))
                {
                    key.SetName(keyName.ToSnakeCase());
                }
            }

            foreach (IMutableForeignKey foreignKey in entity.GetForeignKeys())
            {
                string? fkName = foreignKey.GetConstraintName();
                if (!string.IsNullOrWhiteSpace(fkName))
                {
                    foreignKey.SetConstraintName(fkName.ToSnakeCase());
                }
            }

            foreach (IMutableIndex index in entity.GetIndexes())
            {
                string? indexName = index.GetDatabaseName();
                if (!string.IsNullOrWhiteSpace(indexName))
                {
                    index.SetDatabaseName(indexName.ToSnakeCase());
                }
            }

            // Centralized Audit configuration for entities inheriting from Audit base class
            if (!typeof(Audit).IsAssignableFrom(entity.ClrType)) continue;

            modelBuilder.Entity(entity.ClrType)
                        .Property(nameof(Audit.AppCreatedAt))
                        .IsRequired()
                        .HasColumnType("datetimeoffset(0)")
                        .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            modelBuilder.Entity(entity.ClrType)
                        .Property(nameof(Audit.AppUpdatedAt))
                        .IsRequired()
                        .HasColumnType("datetimeoffset(0)")
                        .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            modelBuilder.Entity(entity.ClrType).HasIndex(nameof(Audit.AppUpdatedAt));
        }
    }
}
