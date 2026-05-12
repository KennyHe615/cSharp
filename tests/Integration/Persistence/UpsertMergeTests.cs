using System.Diagnostics.CodeAnalysis;

using Infrastructure.ExternalApis.Providers.Genesys.Enums;
using Infrastructure.Persistence;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.UserDetails;

using Microsoft.EntityFrameworkCore;

using tests.TestSupport.Persistence;
using tests.TestSupport.Time;

using Xunit;


namespace tests.Integration.Persistence;

/// <summary>
/// Verifies merge-based upsert behavior for caller-defined latest-state rules.
/// </summary>
public sealed class UpsertMergeTests
{
    [Fact]
    public async Task UpsertRangeWithMergeAsync_WhenExistingRowMatches_AppliesMergeCallbackWithoutBlindOverwrite()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        await using AppDbContext db = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);
        UnitOfWork uow = PersistenceTestFactory.CreatePersistenceUnitOfWork(db, dateTimeProvider);

        Guid userId = Guid.NewGuid();
        DateTimeOffset startTime = new DateTimeOffset(2026,
                                                      2,
                                                      26,
                                                      10,
                                                      0,
                                                      0,
                                                      TimeSpan.FromHours(-5));

        PrimaryPresenceEntity existing = new PrimaryPresenceEntity
                                         {
                                             UserId = userId,
                                             StartTime = startTime,
                                             EndTime = startTime.AddMinutes(20),
                                             DurationInSeconds = 1200,
                                             SystemPresence = SystemPresence.OnQueue,
                                             OrganizationPresenceId = "existing"
                                         };

        db.Set<PrimaryPresenceEntity>()
          .Add(existing);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        PrimaryPresenceEntity incoming = new PrimaryPresenceEntity
                                         {
                                             UserId = userId,
                                             StartTime = startTime,
                                             EndTime = null,
                                             DurationInSeconds = 0,
                                             SystemPresence = SystemPresence.Offline,
                                             OrganizationPresenceId = "incoming"
                                         };

        await uow.UpsertRangeWithMergeAsync([incoming], (current, _) => current.OrganizationPresenceId = "merged");
        await uow.SaveChangesAsync();

        PrimaryPresenceEntity row = await db.Set<PrimaryPresenceEntity>()
                                            .SingleAsync(x => x.UserId == userId && x.StartTime == startTime);

        Assert.Equal(startTime.AddMinutes(20), row.EndTime);
        Assert.Equal(1200, row.DurationInSeconds);
        Assert.Equal(SystemPresence.OnQueue, row.SystemPresence);
        Assert.Equal("merged", row.OrganizationPresenceId);
    }

    /// <summary>
    /// Verifies merge upsert inserts a new row without invoking the matched-row merge callback.
    /// </summary>
    [Fact]
    public async Task UpsertRangeWithMergeAsync_WhenIncomingRowIsNew_InsertsRow()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        await using AppDbContext db = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);
        UnitOfWork uow = PersistenceTestFactory.CreatePersistenceUnitOfWork(db, dateTimeProvider);

        Guid userId = Guid.NewGuid();
        DateTimeOffset startTime = new DateTimeOffset(2026,
                                                      2,
                                                      26,
                                                      10,
                                                      0,
                                                      0,
                                                      TimeSpan.FromHours(-5));

        PrimaryPresenceEntity incoming = new PrimaryPresenceEntity
                                         {
                                             UserId = userId,
                                             StartTime = startTime,
                                             EndTime = startTime.AddMinutes(5),
                                             DurationInSeconds = 300,
                                             SystemPresence = SystemPresence.Available,
                                             OrganizationPresenceId = "new"
                                         };

        await uow.UpsertRangeWithMergeAsync([incoming], ThrowIfMergeCallbackRuns);
        await uow.SaveChangesAsync();

        PrimaryPresenceEntity row = await db.Set<PrimaryPresenceEntity>()
                                            .SingleAsync(x => x.UserId == userId && x.StartTime == startTime);

        Assert.Equal(startTime.AddMinutes(5), row.EndTime);
        Assert.Equal(300, row.DurationInSeconds);
        Assert.Equal(SystemPresence.Available, row.SystemPresence);
        Assert.Equal("new", row.OrganizationPresenceId);
    }

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private static void ThrowIfMergeCallbackRuns(PrimaryPresenceEntity current, PrimaryPresenceEntity incoming)
    {
        throw new InvalidOperationException("The merge callback should not run when merge upsert inserts a new row.");
    }

    #endregion
}
