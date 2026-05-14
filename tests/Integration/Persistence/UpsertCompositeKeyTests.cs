using Infrastructure.ExternalApis.Providers.Genesys.Enums;
using Infrastructure.Persistence;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.UserDetails;

using Microsoft.EntityFrameworkCore;

using tests.TestSupport.Persistence;
using tests.TestSupport.Time;

using Xunit;


namespace tests.Integration.Persistence;

public sealed class UpsertCompositeKeyTests
{
    /// <summary>
    /// Verifies range upsert behavior for entities with composite primary keys.
    /// </summary>
    [Fact]
    public async Task UpsertRange_CompositeKey_UpdatesExistingAndAddsNew()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        await using AppDbContext db = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);
        UnitOfWork uow = PersistenceTestFactory.CreatePersistenceUnitOfWork(db, dateTimeProvider);

        Guid userId = Guid.NewGuid();

        DateTimeOffset k1Utc = new DateTimeOffset(2026,
                                                  2,
                                                  26,
                                                  15,
                                                  0,
                                                  0,
                                                  TimeSpan.Zero);
        DateTimeOffset k2Utc = new DateTimeOffset(2026,
                                                  2,
                                                  26,
                                                  15,
                                                  30,
                                                  0,
                                                  TimeSpan.Zero);

        DateTimeOffset k1Eastern = new DateTimeOffset(2026,
                                                      2,
                                                      26,
                                                      10,
                                                      0,
                                                      0,
                                                      TimeSpan.FromHours(-5));
        DateTimeOffset k2Eastern = new DateTimeOffset(2026,
                                                      2,
                                                      26,
                                                      10,
                                                      30,
                                                      0,
                                                      TimeSpan.FromHours(-5));

        PrimaryPresenceEntity existing = new PrimaryPresenceEntity
                                         {
                                             UserId = userId,
                                             StartTimeUtc = k1Utc,
                                             EndTimeUtc = k1Utc.AddMinutes(15),
                                             StartTimeEastern = k1Eastern,
                                             SystemPresence = SystemPresence.Available,
                                             OrganizationPresenceId = "orig"
                                         };

        db.Set<PrimaryPresenceEntity>()
          .Add(existing);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        List<PrimaryPresenceEntity> incoming =
        [
            new PrimaryPresenceEntity
            {
                UserId = userId,
                StartTimeUtc = k1Utc,
                EndTimeUtc = k1Utc.AddMinutes(20),
                StartTimeEastern = k1Eastern,
                SystemPresence =
                        SystemPresence.OnQueue,
                OrganizationPresenceId = "updated"
            },
            new PrimaryPresenceEntity
            {
                UserId = userId,
                StartTimeUtc = k2Utc,
                EndTimeUtc = k2Utc.AddMinutes(10),
                StartTimeEastern = k2Eastern,
                SystemPresence = SystemPresence.Busy,
                OrganizationPresenceId = "new"
            }
        ];

        await uow.UpsertRangeAsync(incoming);
        await uow.SaveChangesAsync();

        List<PrimaryPresenceEntity> rows = await db.Set<PrimaryPresenceEntity>()
                                                   .Where(x => x.UserId == userId)
                                                   .OrderBy(x => x.StartTimeUtc)
                                                   .ToListAsync();

        Assert.Equal(2, rows.Count);

        PrimaryPresenceEntity updated = rows.Single(x => x.StartTimeUtc == k1Utc);
        Assert.Equal(k1Utc.AddMinutes(20), updated.EndTimeUtc);
        Assert.Equal(k1Eastern, updated.StartTimeEastern);
        Assert.Equal(SystemPresence.OnQueue, updated.SystemPresence);
        Assert.Equal("updated", updated.OrganizationPresenceId);

        PrimaryPresenceEntity inserted = rows.Single(x => x.StartTimeUtc == k2Utc);
        Assert.Equal(k2Utc.AddMinutes(10), inserted.EndTimeUtc);
        Assert.Equal(k2Eastern, inserted.StartTimeEastern);
        Assert.Equal(SystemPresence.Busy, inserted.SystemPresence);
        Assert.Equal("new", inserted.OrganizationPresenceId);
    }
}
