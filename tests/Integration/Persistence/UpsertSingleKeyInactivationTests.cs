using Infrastructure.ExternalApis.Providers.Genesys.Enums;
using Infrastructure.Persistence;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.References;

using Microsoft.EntityFrameworkCore;

using tests.TestSupport.Persistence;
using tests.TestSupport.Time;

using Xunit;


namespace tests.Integration.Persistence;

public sealed class UpsertSingleKeyInactivationTests
{
    /// <summary>
    /// Verifies range upsert behavior for single-key reference entities when missing rows are inactivated.
    /// </summary>
    [Fact]
    public async Task UpsertRange_SingleKey_WithInactivationCallback_UpdatesAndInactivatesMissingRows()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        await using AppDbContext db = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);
        UnitOfWork uow = PersistenceTestFactory.CreatePersistenceUnitOfWork(db, dateTimeProvider);

        Skill keepId = new Skill { Id = Guid.NewGuid(), Name = "keep", State = State.Active };
        Skill missingId = new Skill { Id = Guid.NewGuid(), Name = "missing", State = State.Active };

        db.Set<Skill>()
          .AddRange(keepId, missingId);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        List<Skill> incoming =
        [
            new Skill { Id = keepId.Id, Name = "keep-updated", State = State.Active }
        ];

        await uow.UpsertRangeAsync(incoming, s => s.State = State.Inactive);
        await uow.SaveChangesAsync();

        List<Skill> rows = await db.Set<Skill>()
                                   .OrderBy(x => x.Name)
                                   .ToListAsync();

        Skill updated = rows.Single(x => x.Id     == keepId.Id);
        Skill inactivated = rows.Single(x => x.Id == missingId.Id);

        Assert.Equal("keep-updated", updated.Name);
        Assert.Equal(State.Active, updated.State);

        Assert.Equal(State.Inactive, inactivated.State);
    }
}
