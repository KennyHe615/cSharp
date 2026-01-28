using Application.Shared.Context;
using Application.Shared.Extensions;

using Configuration.Options;

using Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;


namespace Infrastructure.Persistence.DbContext;

public class FunctionAppDbContext(DbContextOptions<FunctionAppDbContext> options,
                                  IOptions<DatabaseOptions> databaseOptions,
                                  ILobContext lobContext) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Dynamic connection string resolution based on current LOB Context
        if (optionsBuilder.IsConfigured) return;

        DatabaseOptions dbOptions = databaseOptions.Value;

        // 1. ILobContext is scoped and initialized by SyncOrchestrator for each run.
        string connectionString = lobContext.DatabaseConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Critical: Database ConnectionString for LOB '{lobContext.LobName}' is missing.");
        }

        // 2. Use global settings for Retry and Timeout
        optionsBuilder.UseSqlServer(connectionString,
                                    sqlOptions =>
                                    {
                                        sqlOptions.EnableRetryOnFailure(dbOptions.MaxRetryCount);
                                        sqlOptions.CommandTimeout(dbOptions.CommandTimeout);
                                    });

        if (dbOptions.EnableDetailedErrors) optionsBuilder.EnableDetailedErrors();
        if (dbOptions.EnableSensitiveDataLogging) optionsBuilder.EnableSensitiveDataLogging();
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

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        foreach (EntityEntry<AuditEntity> entry in ChangeTracker.Entries<AuditEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.AppCreatedAt = now;

                    break;
                case EntityState.Modified:
                    entry.Entity.AppUpdatedAt = now;

                    // Ensure created values are immutable
                    entry.Property(e => e.AppCreatedAt).IsModified = false;

                    break;

                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                default:
                    break;
            }
        }

        return base.SaveChangesAsync(ct);
    }
}
