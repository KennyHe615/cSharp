using Application.Abstractions.Context;
using Application.Abstractions.Persistence;

using Infrastructure.ExternalApis.Providers.Genesys.Enums;
using Infrastructure.Persistence;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.UserDetails;
using Infrastructure.Persistence.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using SharedKernel.Time;

using tests.TestSupport.Context;
using tests.TestSupport.Time;

using Xunit;


namespace Tests.Integration.Persistence;

public sealed class UpsertCompositeKeyTests
{
    [Fact]
    public async Task UpsertRange_CompositeKey_UpdatesExistingAndAddsNew()
    {
        ServiceCollection services = new ServiceCollection();

        services.AddLogging();

        services.AddOptions<DatabaseOptions>()
                .Configure(o =>
                           {
                               o.MaxRetryCount = 3;
                               o.CommandTimeout = 30;
                           });

        services.AddSingleton<IDateTimeProvider, FixedEstDateTimeProvider>();
        services.AddScoped<ILobContext, StubLobContext>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase($"upsert-composite-{Guid.NewGuid()}"));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        await using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateAsyncScope();

        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Guid userId = Guid.NewGuid();
        DateTimeOffset k1 = new DateTimeOffset(2026,
                                               2,
                                               26,
                                               10,
                                               0,
                                               0,
                                               TimeSpan.FromHours(-5));
        DateTimeOffset k2 = new DateTimeOffset(2026,
                                               2,
                                               26,
                                               10,
                                               30,
                                               0,
                                               TimeSpan.FromHours(-5));

        PrimaryPresenceEntity existing = new PrimaryPresenceEntity
                                         {
                                             UserId = userId,
                                             StartTime = k1,
                                             EndTime = k1.AddMinutes(15),
                                             DurationInSeconds = 900,
                                             SystemPresence = SystemPresence.Available,
                                             OrganizationPresenceId = "orig"
                                         };

        db.Set<PrimaryPresenceEntity>()
          .Add(existing);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        List<PrimaryPresenceEntity> incoming =
        [
            // same composite key -> update
            new PrimaryPresenceEntity
            {
                UserId = userId,
                StartTime = k1,
                EndTime = k1.AddMinutes(20),
                DurationInSeconds = 1200,
                SystemPresence =
                    SystemPresence.OnQueue,
                OrganizationPresenceId = "updated"
            },
            // new composite key -> insert
            new PrimaryPresenceEntity
            {
                UserId = userId,
                StartTime = k2,
                EndTime = k2.AddMinutes(10),
                DurationInSeconds = 600,
                SystemPresence = SystemPresence.Busy,
                OrganizationPresenceId = "new"
            }
        ];

        await uow.UpsertRangeAsync(incoming);
        await uow.SaveChangesAsync();

        List<PrimaryPresenceEntity> rows = await db.Set<PrimaryPresenceEntity>()
                                                   .Where(x => x.UserId == userId)
                                                   .OrderBy(x => x.StartTime)
                                                   .ToListAsync();

        Assert.Equal(2, rows.Count);

        PrimaryPresenceEntity updated = rows.Single(x => x.StartTime == k1);
        Assert.Equal(1200, updated.DurationInSeconds);
        Assert.Equal(SystemPresence.OnQueue, updated.SystemPresence);
        Assert.Equal("updated", updated.OrganizationPresenceId);

        PrimaryPresenceEntity inserted = rows.Single(x => x.StartTime == k2);
        Assert.Equal(600, inserted.DurationInSeconds);
        Assert.Equal(SystemPresence.Busy, inserted.SystemPresence);
        Assert.Equal("new", inserted.OrganizationPresenceId);
    }
}
