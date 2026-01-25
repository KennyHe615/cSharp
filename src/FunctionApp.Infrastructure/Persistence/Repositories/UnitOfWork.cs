using FunctionApp.Domain.Repositories;
using FunctionApp.Infrastructure.Persistence.DbContext;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;


namespace FunctionApp.Infrastructure.Persistence.Repositories;

public class UnitOfWork(FunctionAppDbContext dbContext) : IUnitOfWork
{
    public async Task UpsertAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        await UpsertRangeAsync([entity], cancellationToken);
    }

    public async Task UpsertRangeAsync<TEntity>(IEnumerable<TEntity> entities,
                                                CancellationToken cancellationToken = default) where TEntity : class
    {
        IEntityType entityType = dbContext.Model.FindEntityType(typeof(TEntity)) ??
                                 throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} not found.");

        IKey primaryKey = entityType.FindPrimaryKey() ??
                          throw new InvalidOperationException($"Primary key not defined for {typeof(TEntity).Name}.");

        DbSet<TEntity> dbSet = dbContext.Set<TEntity>();

        foreach (TEntity entity in entities)
        {
            object?[] keyValues = primaryKey.Properties
                                            .Select(p => entityType.FindProperty(p.Name)!.GetGetter()
                                                                   .GetClrValue(entity))
                                            .ToArray();

            TEntity? existing = await dbSet.FindAsync(keyValues, cancellationToken);

            if (existing == null)
            {
                await dbSet.AddAsync(entity, cancellationToken);
            }
            else
            {
                dbContext.Entry(existing).CurrentValues.SetValues(entity);
            }
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }
}
