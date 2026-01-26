using FunctionApp.Application.Shared.Context;
using FunctionApp.Application.Shared.Extensions;
using FunctionApp.Configuration.Options;
using FunctionApp.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.Persistence.DbContext;

public class FunctionAppDbContext(DbContextOptions<FunctionAppDbContext> options,
                                  ILobContext lobContext,
                                  IOptions<MultiLobOptions> multiLobOptions)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Dynamic connection string resolution based on current LOB Context
        if (optionsBuilder.IsConfigured) return;

        MultiLobOptions globalOptions = multiLobOptions.Value;
        LobSettings? settings = lobContext.LobSettings;

        if (string.IsNullOrEmpty(lobContext.LobName) || settings == null)
        {
            throw new InvalidOperationException(
                $"Critical: LOB configuration for '{lobContext.LobName ?? "Unknown"}' not found or context is not initialized.");
        }

        // Merge and Validate Connection String (Must be in LOB settings)
        string connectionString = settings.DatabaseConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Critical: Database ConnectionString for LOB '{lobContext.LobName}' is missing.");
        }

        // 2. Use global settings for Retry and Timeout
        optionsBuilder.UseSqlServer(connectionString,
                                    sqlOptions =>
                                    {
                                        sqlOptions.EnableRetryOnFailure(globalOptions.DatabaseMaxRetryCount);
                                        sqlOptions.CommandTimeout(globalOptions.DatabaseCommandTimeout);
                                    });

        if (globalOptions.DatabaseEnableDetailedErrors) optionsBuilder.EnableDetailedErrors();
        if (globalOptions.DatabaseEnableSensitiveDataLogging) optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This automatically finds all classes in this assembly that implement IEntityTypeConfiguration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FunctionAppDbContext).Assembly);

        // Apply naming convention for all entities and properties
        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
        {
            // Set table name to snake_case
            entity.SetTableName(entity.GetTableName().ToSnakeCase());

            // Set column names to snake_case
            foreach (IMutableProperty property in entity.GetProperties())
            {
                // Use the property name directly to generate the column name
                property.SetColumnName(property.Name.ToSnakeCase());
            }

            // Set key and index names to snake_case
            foreach (IMutableKey key in entity.GetKeys())
            {
                key.SetName(key.GetName()!.ToSnakeCase());
            }

            foreach (IMutableForeignKey foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(foreignKey.GetConstraintName().ToSnakeCase());
            }

            foreach (IMutableIndex index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName().ToSnakeCase());
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<EntityEntry> entries = ChangeTracker.Entries()
                                                        .Where(e => e is
                                                        {
                                                            Entity: AuditEntity,
                                                            State: EntityState.Added or EntityState.Modified
                                                        });

        DateTimeOffset now = DateTimeOffset.Now;

        foreach (EntityEntry entityEntry in entries)
        {
            AuditEntity entity = (AuditEntity)entityEntry.Entity;
            entity.AppUpdatedAt = now;

            if (entityEntry.State == EntityState.Added)
            {
                entity.AppCreatedAt = now;
            }
            else
            {
                // Ensure AppCreatedAt is never modified during an update
                entityEntry.Property(nameof(AuditEntity.AppCreatedAt)).IsModified = false;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
