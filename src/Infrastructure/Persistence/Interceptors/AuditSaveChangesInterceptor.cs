using Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

using SharedKernel.Time;


namespace Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core <see cref="SaveChangesInterceptor"/> that automatically stamps audit fields for
/// tracked entities inheriting from <see cref="Audit"/>.
/// </summary>
public sealed class AuditSaveChangesInterceptor(IDateTimeProvider dateTimeProvider) : SaveChangesInterceptor
{
    /// <summary>
    /// Intercepts synchronous SaveChanges and applies audit timestamps before commit.
    /// </summary>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Intercepts asynchronous SaveChanges and applies audit timestamps before commit.
    /// </summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    #region ========== *** Private Methods *** ==========

    private void ApplyAudit(Microsoft.EntityFrameworkCore.DbContext? context)
    {
        if (context is null) return;

        if (!context.ChangeTracker.AutoDetectChangesEnabled) context.ChangeTracker.DetectChanges();

        DateTimeOffset now = dateTimeProvider.EstNowOffset;

        foreach (EntityEntry<Audit> entry in context.ChangeTracker.Entries<Audit>())
        {
            if (entry.State is EntityState.Detached or EntityState.Deleted) continue;

            entry.Entity.AppUpdatedAt = now;
            entry.Property(e => e.AppUpdatedAt)
                 .IsModified = true;

            if (entry.State is EntityState.Added)
            {
                entry.Entity.AppCreatedAt = now;
            }
            else
            {
                entry.Property(e => e.AppCreatedAt)
                     .IsModified = false;
            }
        }
    }

    #endregion
}
