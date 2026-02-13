using Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Shared.Time;


namespace Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core <see cref="SaveChangesInterceptor"/> that automatically stamps audit fields for entities
/// tracked by the current <see cref="DbContext"/> that derive from <see cref="Audit"/>.
/// </summary>
/// <remarks><list type="bullet">
/// <item>Uses <see cref="IDateTimeProvider"/> so timestamps are consistent and testable.</item>
/// <item>When <see cref="ChangeTracker.AutoDetectChangesEnabled"/> is disabled, this interceptor calls
/// <see cref="ChangeTracker.DetectChanges"/> to ensure entity states and modified properties are up to date.</item>
/// <item>Current behavior sets <c>AppUpdatedAt</c> for all tracked <see cref="Audit"/> entities except
/// <see cref="EntityState.Detached"/> and <see cref="EntityState.Deleted"/>; for <see cref="EntityState.Added"/>,
/// it also sets <c>AppCreatedAt</c>.</item>
/// </list></remarks>
public sealed class AuditSaveChangesInterceptor(IDateTimeProvider dateTimeProvider) : SaveChangesInterceptor
{
    /// <summary>
    /// Intercepts synchronous <c>SaveChanges</c> and applies audit timestamps before changes are written.
    /// </summary>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Intercepts asynchronous <c>SaveChanges</c> and applies audit timestamps before changes are written.
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

    /// <summary>
    /// Applies audit values to all tracked <see cref="Audit"/> entities in the given <see cref="DbContext"/>.
    /// </summary>
    /// <param name="context">The current <see cref="DbContext"/>; may be <c>null</c>.</param>
    /// <remarks><list type="bullet">
    /// <item>Skips entities in <see cref="EntityState.Detached"/> and <see cref="EntityState.Deleted"/>.</item>
    /// <item>Marks <c>AppUpdatedAt</c> as modified to ensure it is persisted.</item>
    /// <item>Sets <c>AppCreatedAt</c> only for <see cref="EntityState.Added"/> entities and prevents updating it for others.</item>
    /// </list></remarks>
    private void ApplyAudit(DbContext? context)
    {
        if (context is null) return;

        // Ensure states are up-to-date if AutoDetectChanges is disabled.
        if (!context.ChangeTracker.AutoDetectChangesEnabled)
        {
            context.ChangeTracker.DetectChanges();
        }

        DateTimeOffset now = dateTimeProvider.EstNowOffset;

        foreach (EntityEntry<Audit> entry in context.ChangeTracker.Entries<Audit>())
        {
            if (entry.State is EntityState.Detached or EntityState.Deleted) continue;

            // Always bump updated time (even for Unchanged).
            entry.Entity.AppUpdatedAt = now;
            entry.Property(e => e.AppUpdatedAt).IsModified = true;

            // CreatedAt only on Add.
            if (entry.State is EntityState.Added)
            {
                entry.Entity.AppCreatedAt = now;
            }
            else
            {
                entry.Property(e => e.AppCreatedAt).IsModified = false;
            }
        }
    }

    #endregion
}
