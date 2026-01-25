using FunctionApp.Application.Shared.Extensions;
using FunctionApp.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;


namespace FunctionApp.Infrastructure.Persistence.DbContext;

public class FunctionAppDbContext(DbContextOptions<FunctionAppDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
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
}
