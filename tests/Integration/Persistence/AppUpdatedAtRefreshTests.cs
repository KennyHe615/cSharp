using Infrastructure.Persistence;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.References;
using Infrastructure.Persistence.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using tests.TestSupport.Context;
using tests.TestSupport.Time;

using Xunit;


namespace tests.Integration.Persistence;

public sealed class AppUpdatedAtRefreshTests
{
    [Fact]
    public async Task SaveChanges_WhenEntityIsUnchanged_StillRefreshesAppUpdatedAt()
    {
        DateTimeOffset t1 = new DateTimeOffset(2026,
                                               2,
                                               26,
                                               10,
                                               0,
                                               0,
                                               TimeSpan.FromHours(-5));
        DateTimeOffset t2 = t1.AddMinutes(5);

        SequenceEstDateTimeProvider dateTimeProvider = new SequenceEstDateTimeProvider([t1, t2]);
        AuditSaveChangesInterceptor interceptor = new AuditSaveChangesInterceptor(dateTimeProvider);

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                .UseInMemoryDatabase($"app-updated-at-{Guid.NewGuid()}")
                                                .Options;

        await using AppDbContext db =
            new AppDbContext(options,
                             Options.Create(new DatabaseOptions()),
                             new StubLobContext(),
                             dateTimeProvider,
                             interceptor);

        Skill entity = new Skill
                       {
                           Id = Guid.NewGuid(),
                           Name = "skill-a"
                       };

        db.Set<Skill>()
          .Add(entity);
        await db.SaveChangesAsync();

        DateTimeOffset firstUpdatedAt = entity.AppUpdatedAt;

        db.ChangeTracker.Clear();

        Skill loaded = await db.Set<Skill>()
                               .SingleAsync(x => x.Id == entity.Id);
        Assert.Equal(EntityState.Unchanged,
                     db.Entry(loaded)
                       .State);

        await db.SaveChangesAsync();

        DateTimeOffset secondUpdatedAt = loaded.AppUpdatedAt;

        Assert.True(secondUpdatedAt > firstUpdatedAt);
        Assert.Equal(TimeSpan.FromHours(-5), secondUpdatedAt.Offset);
    }
}
