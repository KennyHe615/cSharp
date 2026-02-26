using Application.Abstractions.Context;
using Application.Abstractions.Persistence;

using Infrastructure.ExternalApis.Genesys.Models.Enums;
using Infrastructure.Persistence;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.References;
using Infrastructure.Persistence.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using SharedKernel.Time;

using tests.TestSupport.Context;
using tests.TestSupport.Time;

using Xunit;


namespace Tests.Integration.Persistence;

public sealed class UpsertSingleKeyInactivationTests
{
    [Fact]
    public async Task UpsertRange_SingleKey_WithInactivationCallback_UpdatesAndInactivatesMissingRows()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDateTimeProvider, FixedEstDateTimeProvider>();
        services.AddScoped<ILobContext, StubLobContext>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddOptions<DatabaseOptions>()
                .Configure(o =>
                           {
                               o.MaxRetryCount = 3;
                               o.CommandTimeout = 30;
                               o.EnableDetailedErrors = false;
                               o.EnableSensitiveDataLogging = false;
                           });
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase($"upsert-single-{Guid.NewGuid()}"));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        await using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateAsyncScope();

        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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
