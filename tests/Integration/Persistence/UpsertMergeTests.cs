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
    /// <summary>
    /// Verifies range merge upsert applies the matched-row callback without blind overwrite.
    /// </summary>
    [Fact]
    public async Task UpsertRangeWithMergeAsync_WhenExistingRowMatches_AppliesMergeCallbackWithoutBlindOverwrite()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        await using AppDbContext db = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);
        UnitOfWork uow = PersistenceTestFactory.CreatePersistenceUnitOfWork(db, dateTimeProvider);

        Guid userId = Guid.NewGuid();
        DateTimeOffset startTimeUtc = new DateTimeOffset(2026,
                                                         2,
                                                         26,
                                                         15,
                                                         0,
                                                         0,
                                                         TimeSpan.Zero);
        DateTimeOffset startTimeEastern = new DateTimeOffset(2026,
                                                             2,
                                                             26,
                                                             10,
                                                             0,
                                                             0,
                                                             TimeSpan.FromHours(-5));

        PrimaryPresenceEntity existing = new PrimaryPresenceEntity
                                         {
                                             UserId = userId,
                                             StartTimeUtc = startTimeUtc,
                                             EndTimeUtc = startTimeUtc.AddMinutes(20),
                                             StartTimeEastern = startTimeEastern,
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
                                             StartTimeUtc = startTimeUtc,
                                             EndTimeUtc = null,
                                             StartTimeEastern =
                                                     startTimeEastern.AddMinutes(1),
                                             SystemPresence = SystemPresence.Offline,
                                             OrganizationPresenceId = "incoming"
                                         };

        await uow.UpsertRangeWithMergeAsync([incoming], (current, _) => current.OrganizationPresenceId = "merged");
        await uow.SaveChangesAsync();

        PrimaryPresenceEntity row = await db.Set<PrimaryPresenceEntity>()
                                            .SingleAsync(x => x.UserId == userId && x.StartTimeUtc == startTimeUtc);

        Assert.Equal(startTimeUtc.AddMinutes(20), row.EndTimeUtc);
        Assert.Equal(startTimeEastern, row.StartTimeEastern);
        Assert.Equal(SystemPresence.OnQueue, row.SystemPresence);
        Assert.Equal("merged", row.OrganizationPresenceId);
    }

    /// <summary>
    /// Verifies single-entity merge upsert applies the matched-row callback.
    /// </summary>
    [Fact]
    public async Task UpsertWithMergeAsync_WhenExistingRowMatches_AppliesMergeCallback()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        await using AppDbContext db = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);
        UnitOfWork uow = PersistenceTestFactory.CreatePersistenceUnitOfWork(db, dateTimeProvider);

        Guid userId = Guid.NewGuid();
        DateTimeOffset startTimeUtc = new DateTimeOffset(2026,
                                                         2,
                                                         26,
                                                         15,
                                                         0,
                                                         0,
                                                         TimeSpan.Zero);
        DateTimeOffset startTimeEastern = new DateTimeOffset(2026,
                                                             2,
                                                             26,
                                                             10,
                                                             0,
                                                             0,
                                                             TimeSpan.FromHours(-5));

        PrimaryPresenceEntity existing = new PrimaryPresenceEntity
                                         {
                                             UserId = userId,
                                             StartTimeUtc = startTimeUtc,
                                             EndTimeUtc = startTimeUtc.AddMinutes(10),
                                             StartTimeEastern = startTimeEastern,
                                             SystemPresence = SystemPresence.Available,
                                             OrganizationPresenceId = "existing"
                                         };

        db.Set<PrimaryPresenceEntity>()
          .Add(existing);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        PrimaryPresenceEntity incoming = new PrimaryPresenceEntity
                                         {
                                             UserId = userId,
                                             StartTimeUtc = startTimeUtc,
                                             EndTimeUtc = startTimeUtc.AddMinutes(30),
                                             StartTimeEastern = startTimeEastern,
                                             SystemPresence = SystemPresence.OnQueue,
                                             OrganizationPresenceId = "incoming"
                                         };

        await uow.UpsertWithMergeAsync(incoming,
                                       (current, next) =>
                                       {
                                           current.EndTimeUtc = next.EndTimeUtc;
                                           current.StartTimeEastern = next.StartTimeEastern;
                                           current.SystemPresence = next.SystemPresence;
                                           current.OrganizationPresenceId = "single-merge";
                                       });
        await uow.SaveChangesAsync();

        PrimaryPresenceEntity row = await db.Set<PrimaryPresenceEntity>()
                                            .SingleAsync(x => x.UserId == userId && x.StartTimeUtc == startTimeUtc);

        Assert.Equal(startTimeUtc.AddMinutes(30), row.EndTimeUtc);
        Assert.Equal(startTimeEastern, row.StartTimeEastern);
        Assert.Equal(SystemPresence.OnQueue, row.SystemPresence);
        Assert.Equal("single-merge", row.OrganizationPresenceId);
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
        DateTimeOffset startTimeUtc = new DateTimeOffset(2026,
                                                         2,
                                                         26,
                                                         15,
                                                         0,
                                                         0,
                                                         TimeSpan.Zero);
        DateTimeOffset startTimeEastern = new DateTimeOffset(2026,
                                                             2,
                                                             26,
                                                             10,
                                                             0,
                                                             0,
                                                             TimeSpan.FromHours(-5));

        PrimaryPresenceEntity incoming = new PrimaryPresenceEntity
                                         {
                                             UserId = userId,
                                             StartTimeUtc = startTimeUtc,
                                             EndTimeUtc = startTimeUtc.AddMinutes(5),
                                             StartTimeEastern = startTimeEastern,
                                             SystemPresence = SystemPresence.Available,
                                             OrganizationPresenceId = "new"
                                         };

        await uow.UpsertRangeWithMergeAsync([incoming], ThrowIfMergeCallbackRuns);
        await uow.SaveChangesAsync();

        PrimaryPresenceEntity row = await db.Set<PrimaryPresenceEntity>()
                                            .SingleAsync(x => x.UserId == userId && x.StartTimeUtc == startTimeUtc);

        Assert.Equal(startTimeUtc.AddMinutes(5), row.EndTimeUtc);
        Assert.Equal(startTimeEastern, row.StartTimeEastern);
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
