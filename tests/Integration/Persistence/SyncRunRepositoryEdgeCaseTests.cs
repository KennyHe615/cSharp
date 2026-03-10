using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;
using Infrastructure.Persistence.Repositories.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using tests.TestSupport.Persistence;

using Xunit;


namespace tests.Integration.Persistence;

public sealed class SyncRunRepositoryEdgeCaseTests
{
    [Fact]
    public async Task StartNewRunAsync_RequestNotFound_ThrowsInvalidOperationException()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRunRepository sut = SyncTrackingPersistenceTestFixture.CreateRunRepository(scope.ServiceProvider, db);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.StartNewRunAsync(9999L,
                                                                 CancellationToken.None));

        Assert.Contains("Sync request '9999' was not found.", ex.Message);
    }

    [Fact]
    public async Task MarkCompletedAsync_RunNotFound_ThrowsInvalidOperationException()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRunRepository sut = SyncTrackingPersistenceTestFixture.CreateRunRepository(scope.ServiceProvider, db);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.MarkCompletedAsync(7777L,
                                                                 CancellationToken.None));

        Assert.Contains("Sync run '7777' was not found.", ex.Message);
    }

    [Fact]
    public async Task MarkFailedAsync_WhenRunAlreadyCompleted_DoesNotOverwriteStatusOrReason()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRequestEntity request =
            SyncTrackingPersistenceTestFixture.CreateRequest(SyncCategory.UsersDetails, SyncMode.Incremental, "edge-1");
        db.Set<SyncRequestEntity>()
          .Add(request);
        await db.SaveChangesAsync();

        SyncRunRepository sut = SyncTrackingPersistenceTestFixture.CreateRunRepository(scope.ServiceProvider, db);

        long runId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);
        await sut.MarkCompletedAsync(runId, CancellationToken.None);

        SyncRunEntity before = await db.Set<SyncRunEntity>()
                                       .AsNoTracking()
                                       .SingleAsync(x => x.Id == runId);

        await sut.MarkFailedAsync(runId, "should-not-apply", CancellationToken.None);

        SyncRunEntity after = await db.Set<SyncRunEntity>()
                                      .AsNoTracking()
                                      .SingleAsync(x => x.Id == runId);

        Assert.Equal(SyncRunStatus.Completed, after.Status);
        Assert.Equal(before.RunCompletedAt, after.RunCompletedAt);
        Assert.Equal(before.FailureReason, after.FailureReason);
    }

    [Fact]
    public async Task MarkSupersededAsync_WhenRunAlreadyCanceled_DoesNotOverwriteFinalState()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRequestEntity request =
            SyncTrackingPersistenceTestFixture.CreateRequest(SyncCategory.UsersDetails, SyncMode.Recovery, "edge-2");
        db.Set<SyncRequestEntity>()
          .Add(request);
        await db.SaveChangesAsync();

        SyncRunRepository sut = SyncTrackingPersistenceTestFixture.CreateRunRepository(scope.ServiceProvider, db);

        long runId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);
        await sut.MarkCanceledAsync(runId, "cancelled", CancellationToken.None);

        SyncRunEntity before = await db.Set<SyncRunEntity>()
                                       .AsNoTracking()
                                       .SingleAsync(x => x.Id == runId);

        await sut.MarkSupersededAsync(runId, 123456L, CancellationToken.None);

        SyncRunEntity after = await db.Set<SyncRunEntity>()
                                      .AsNoTracking()
                                      .SingleAsync(x => x.Id == runId);

        Assert.Equal(SyncRunStatus.Canceled, after.Status);
        Assert.Equal(before.SupersededByRunId, after.SupersededByRunId);
        Assert.Equal(before.FailureReason, after.FailureReason);
    }
}
