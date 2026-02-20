using Application.Abstractions.Context;

using Infrastructure.Persistence.Entities;
using Infrastructure.Persistence.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

using SharedKernel.Extensions;
using SharedKernel.Time;


namespace Infrastructure.Persistence.DbContext;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options,
                                 IOptions<DatabaseOptions> databaseOptions,
                                 ILobContext lobContext,
                                 IDateTimeProvider dateTimeProvider,
                                 AuditSaveChangesInterceptor auditInterceptor)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Dynamic connection string resolution based on current LOB Context
        if (optionsBuilder.IsConfigured) return;

        DatabaseOptions dbOptions = databaseOptions.Value;

        // 1. ILobContext is scoped and initialized by SyncOrchestrator for each run.
        string connectionString = lobContext.DbConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new
                DbContextConfigurationException($"Database ConnectionString for LOB '{lobContext.LobName}' is missing.");
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This automatically finds all classes in this assembly that implement IEntityTypeConfiguration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplyEstConvention(modelBuilder);

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

            modelBuilder
               .Entity(entity.ClrType)
               .Property(nameof(Audit.AppCreatedAt))
               .IsRequired()
               .HasColumnType("datetimeoffset(0)")
               .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            modelBuilder
               .Entity(entity.ClrType)
               .Property(nameof(Audit.AppUpdatedAt))
               .IsRequired()
               .HasColumnType("datetimeoffset(0)")
               .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            modelBuilder
               .Entity(entity.ClrType)
               .HasIndex(nameof(Audit.AppUpdatedAt));
        }
    }

    // For EF ValueConverter, use static methods to avoid expression-tree closure issues.
    private void ApplyEstConvention(ModelBuilder modelBuilder)
    {
        ValueConverter<DateTimeOffset, DateTimeOffset> dtoConverter =
            new ValueConverter<DateTimeOffset, DateTimeOffset>(v => dateTimeProvider.ConvertToEst(v),
                                                               v => dateTimeProvider.ConvertToEst(v));

        ValueConverter<DateTimeOffset?, DateTimeOffset?> dtoNullableConverter =
            new ValueConverter<DateTimeOffset?, DateTimeOffset?>(v =>
                                                                     v.HasValue
                                                                         ? dateTimeProvider.ConvertToEst(v.Value)
                                                                         : null,
                                                                 v => v.HasValue
                                                                     ? dateTimeProvider.ConvertToEst(v.Value)
                                                                     : null);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetColumnType("datetimeoffset(0)");
                    property.SetValueConverter(dtoConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetColumnType("datetimeoffset(0)");
                    property.SetValueConverter(dtoNullableConverter);
                }
            }

            // Extra safety: ensure all PK DateTimeOffset columns are also forced (some configs override keys).
            foreach (IMutableKey key in entityType.GetKeys())
            {
                foreach (IMutableProperty keyProp in key.Properties.Where(p => p.ClrType == typeof(DateTimeOffset)))
                {
                    keyProp.SetColumnType("datetimeoffset(3)");
                    keyProp.SetValueConverter(dtoConverter);
                }

                foreach (IMutableProperty keyProp in key.Properties.Where(p => p.ClrType == typeof(DateTimeOffset?)))
                {
                    keyProp.SetColumnType("datetimeoffset(3)");
                    keyProp.SetValueConverter(dtoNullableConverter);
                }
            }
        }
    }
}
