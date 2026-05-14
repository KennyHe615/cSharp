using Application.Abstractions.Persistence;
using Application.Abstractions.Persistence.SyncTracking;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Lobs;
using SharedKernel.Time;


namespace Infrastructure.Persistence.Repositories.SyncTracking;

/// <summary>
/// Repository implementation for atomic incremental scheduling window reservation.
/// </summary>
public sealed class IncrementalSyncWindowRepository(AppDbContext dbContext,
                                                    IUnitOfWork uow) : IIncrementalSyncWindowRepository
{
    /// <inheritdoc />
    public async Task<IncrementalSyncWindowReservation> ReserveNextWindowAsync(LobName lob,
                                                                               SyncAnalyticsCategory category,
                                                                               DateTimeOffset intervalEndEastern,
                                                                               CancellationToken ct)
    {
        DateTimeOffset intervalEndUtc = intervalEndEastern.TruncateToMinute()
                                                          .ToUniversalTime();

        while (true)
        {
            IncrementalSyncWindowEntity? existing = await dbContext.Set<IncrementalSyncWindowEntity>()
                                                                   .FirstOrDefaultAsync(x => x.Category == category, ct)
                                                                   .ConfigureAwait(false);

            if (existing is null)
            {
                IncrementalSyncWindowReservation? createdReservation =
                        await TryReserveInitialWindowAsync(category,
                                                           intervalEndEastern,
                                                           intervalEndUtc,
                                                           ct)
                               .ConfigureAwait(false);

                if (createdReservation is not null) return createdReservation;

                continue;
            }

            IncrementalSyncWindowReservation? advancedReservation =
                    await TryAdvanceExistingWindowAsync(existing, intervalEndUtc, ct)
                           .ConfigureAwait(false);

            if (advancedReservation is not null) return advancedReservation;
        }
    }

    /// <summary>
    /// Tries to create and reserve the initial incremental window row for the specified category.
    /// Returns <c>null</c> when another concurrent worker won the row-creation race and the caller should retry.
    /// </summary>
    /// <param name="category">Incremental analytics category.</param>
    /// <param name="intervalEndEastern">Current worker cutoff time in Eastern time.</param>
    /// <param name="intervalEndUtc">Normalized interval end in UTC.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The created reservation when successful; otherwise <c>null</c> so the caller can retry.
    /// </returns>
    private async Task<IncrementalSyncWindowReservation?> TryReserveInitialWindowAsync(
            SyncAnalyticsCategory category,
            DateTimeOffset intervalEndEastern,
            DateTimeOffset intervalEndUtc,
            CancellationToken ct)
    {
        DateTimeOffset initialStartUtc = intervalEndEastern.TruncateToMinute()
                                                           .StartOfDay()
                                                           .ToUniversalTime();

        IncrementalSyncWindowEntity created = new IncrementalSyncWindowEntity
                                              {
                                                  Category = category,
                                                  NextIntervalStartUtc = intervalEndUtc,
                                                  LastReservedStartUtc =
                                                          initialStartUtc,
                                                  LastReservedEndUtc = intervalEndUtc
                                              };

        await uow.UpsertAsync(created, ct: ct)
                 .ConfigureAwait(false);

        try
        {
            await uow.SaveChangesAsync(ct)
                     .ConfigureAwait(false);

            return new IncrementalSyncWindowReservation(true,
                                                        new UtcInterval(initialStartUtc, intervalEndUtc).ToString(),
                                                        initialStartUtc,
                                                        intervalEndUtc);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();

            return null;
        }
        catch (DbUpdateException ex) when (UniqueViolationDetector.IsIncrementalSyncWindowCategoryUniqueViolation(ex))
        {
            dbContext.ChangeTracker.Clear();

            return null;
        }
    }

    /// <summary>
    /// Tries to advance an existing incremental window row to the supplied UTC end boundary.
    /// Returns a non-reserved result when no forward progress is available, or <c>null</c>
    /// when a concurrency race requires the caller to retry.
    /// </summary>
    /// <param name="existing">Existing incremental window row.</param>
    /// <param name="intervalEndUtc">Normalized interval end in UTC.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A reservation result when the operation completed; otherwise <c>null</c> so the caller can retry.
    /// </returns>
    private async Task<IncrementalSyncWindowReservation?> TryAdvanceExistingWindowAsync(
            IncrementalSyncWindowEntity existing,
            DateTimeOffset intervalEndUtc,
            CancellationToken ct)
    {
        DateTimeOffset startUtc = existing.NextIntervalStartUtc.NormalizeToUtc();

        if (startUtc >= intervalEndUtc)
        {
            return new IncrementalSyncWindowReservation(false,
                                                        null,
                                                        null,
                                                        null);
        }

        existing.LastReservedStartUtc = startUtc;
        existing.LastReservedEndUtc = intervalEndUtc;
        existing.NextIntervalStartUtc = intervalEndUtc;

        try
        {
            await uow.SaveChangesAsync(ct)
                     .ConfigureAwait(false);

            return new IncrementalSyncWindowReservation(true,
                                                        new UtcInterval(startUtc, intervalEndUtc).ToString(),
                                                        startUtc,
                                                        intervalEndUtc);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();

            return null;
        }
    }
}
