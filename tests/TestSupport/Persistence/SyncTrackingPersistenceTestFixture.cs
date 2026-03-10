using Application.Abstractions.Context;
using Application.Abstractions.Persistence;
using Application.Enums;

using Infrastructure.Persistence;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Repositories.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

using SharedKernel.Time;

using tests.TestSupport.Context;
using tests.TestSupport.Time;


namespace tests.TestSupport.Persistence;

public static class SyncTrackingPersistenceTestFixture
{
    public static ServiceProvider BuildProvider(IDateTimeProvider? dateTimeProvider = null, string? dbName = null)
    {
        ServiceCollection services = [];

        services.AddLogging();

        services.AddOptions<DatabaseOptions>()
                .Configure(o =>
                           {
                               o.MaxRetryCount = 3;
                               o.CommandTimeout = 30;
                           });

        services.AddSingleton<ILobContext, StubLobContext>();
        services.AddSingleton(dateTimeProvider ?? new FixedEstDateTimeProvider());
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>(o => o
                                                .UseInMemoryDatabase(dbName ?? $"sync-tracking-tests-{Guid.NewGuid()}")
                                                .ConfigureWarnings(w => w.Ignore(InMemoryEventId
                                                                      .TransactionIgnoredWarning)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services.BuildServiceProvider();
    }

    public static SyncRunRepository CreateRunRepository(IServiceProvider provider, AppDbContext db)
    {
        IUnitOfWork uow = provider.GetRequiredService<IUnitOfWork>();
        IDateTimeProvider time = provider.GetRequiredService<IDateTimeProvider>();

        return new SyncRunRepository(db, uow, time);
    }

    public static SyncCheckpointRepository CreateCheckpointRepository(IServiceProvider provider, AppDbContext db)
    {
        IUnitOfWork uow = provider.GetRequiredService<IUnitOfWork>();

        return new SyncCheckpointRepository(db, uow);
    }

    public static SyncRequestRepository CreateRequestRepository(IServiceProvider provider, AppDbContext db)
    {
        IUnitOfWork uow = provider.GetRequiredService<IUnitOfWork>();

        return new SyncRequestRepository(db, uow);
    }

    public static SyncRequestEntity CreateRequest(SyncCategory category, SyncMode mode, string interval)
    {
        SyncRequestEntity request = new SyncRequestEntity
                                    {
                                        Category = category,
                                        Mode = mode,
                                        Interval = interval,
                                        PageNumber = null,
                                        GenesysJobId = null
                                    };
        request.RebuildScopeKey();

        return request;
    }
}
