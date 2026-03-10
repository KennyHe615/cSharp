using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;
using Infrastructure.Persistence.Repositories.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using tests.TestSupport.Persistence;

using Xunit;


namespace tests.Integration.Persistence;

public sealed class SyncRequestRepositoryEdgeCaseTests
{
    [Fact]
    public async Task SetCurrentRunAsync_ValidOwnership_UpdatesCurrentRunId()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRequestEntity request =
            SyncTrackingPersistenceTestFixture.CreateRequest(SyncCategory.UsersDetails,
                                                             SyncMode.Incremental,
                                                             "edge-req-1");
        db.Set<SyncRequestEntity>()
          .Add(request);
        await db.SaveChangesAsync();

        SyncRunEntity run = new SyncRunEntity
                            {
                                RequestId = request.Id,
                                Status = SyncRunStatus.Running,
                                AttemptNo = 1,
                                RunStartedAt = DateTimeOffset.UtcNow
                            };
        db.Set<SyncRunEntity>()
          .Add(run);
        await db.SaveChangesAsync();

        SyncRequestRepository sut = SyncTrackingPersistenceTestFixture.CreateRequestRepository(provider, db);

        await sut.SetCurrentRunAsync(request.Id, run.Id, CancellationToken.None);

        SyncRequestEntity reloaded = await db.Set<SyncRequestEntity>()
                                             .SingleAsync(x => x.Id == request.Id);

        Assert.Equal(run.Id, reloaded.CurrentRunId);
    }

    [Fact]
    public async Task SetCurrentRunAsync_RequestNotFound_ThrowsInvalidOperationException()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRequestRepository sut =
            SyncTrackingPersistenceTestFixture.CreateRequestRepository(scope.ServiceProvider, db);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SetCurrentRunAsync(9999L,
                                                                 1L,
                                                                 CancellationToken.None));

        Assert.Contains("Sync request '9999' was not found.", ex.Message);
    }

    [Fact]
    public async Task SetCurrentRunAsync_RunNotFound_ThrowsInvalidOperationException()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRequestEntity request =
            SyncTrackingPersistenceTestFixture.CreateRequest(SyncCategory.UsersDetails,
                                                             SyncMode.Incremental,
                                                             "edge-req-2");
        db.Set<SyncRequestEntity>()
          .Add(request);
        await db.SaveChangesAsync();

        SyncRequestRepository sut =
            SyncTrackingPersistenceTestFixture.CreateRequestRepository(scope.ServiceProvider, db);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SetCurrentRunAsync(request.Id,
                                                                 8888L,
                                                                 CancellationToken.None));

        Assert.Contains("Sync run '8888' was not found.", ex.Message);
    }

    [Fact]
    public async Task SetCurrentRunAsync_RunBelongsToDifferentRequest_ThrowsInvalidOperationException()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRequestEntity requestA =
            SyncTrackingPersistenceTestFixture.CreateRequest(SyncCategory.UsersDetails,
                                                             SyncMode.Incremental,
                                                             "edge-req-a");
        SyncRequestEntity requestB =
            SyncTrackingPersistenceTestFixture.CreateRequest(SyncCategory.UsersDetails,
                                                             SyncMode.Incremental,
                                                             "edge-req-b");

        db.Set<SyncRequestEntity>()
          .AddRange(requestA, requestB);
        await db.SaveChangesAsync();

        SyncRunEntity runOwnedByB = new SyncRunEntity
                                    {
                                        RequestId = requestB.Id,
                                        Status = SyncRunStatus.Running,
                                        AttemptNo = 1,
                                        RunStartedAt = DateTimeOffset.UtcNow
                                    };
        db.Set<SyncRunEntity>()
          .Add(runOwnedByB);
        await db.SaveChangesAsync();

        SyncRequestRepository sut =
            SyncTrackingPersistenceTestFixture.CreateRequestRepository(scope.ServiceProvider, db);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SetCurrentRunAsync(requestA.Id,
                                                                 runOwnedByB.Id,
                                                                 CancellationToken.None));

        Assert.Contains($"Sync run '{runOwnedByB.Id}' does not belong to sync request '{requestA.Id}'.", ex.Message);
    }
}
