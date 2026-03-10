using System.Diagnostics.CodeAnalysis;

using Application.DTOs.SyncTracking;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;
using Infrastructure.Persistence.Repositories.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using tests.TestSupport.Persistence;

using Xunit;


namespace tests.Integration.Persistence;

public sealed class SyncCheckpointRepositoryTests
{
    #region ========== *** UpsertAsync *** ==========

    [Fact]
    public async Task UpsertAsync_NewCheckpoint_InsertsRow()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        long runId = await CreateRunningRunAsync(scope.ServiceProvider, db);

        SyncCheckpointRepository sut =
            SyncTrackingPersistenceTestFixture.CreateCheckpointRepository(scope.ServiceProvider, db);

        await sut.UpsertAsync(runId,
                              "Dispatch",
                              "UsersDetails|Incremental|I1|-|-",
                              SyncRunStatus.Running,
                              null,
                              CancellationToken.None);

        SyncCheckpointEntity row = await db.Set<SyncCheckpointEntity>()
                                           .SingleAsync();

        Assert.Equal(runId, row.RunId);
        Assert.Equal("Dispatch", row.Step);
        Assert.Equal("UsersDetails|Incremental|I1|-|-", row.Cursor);
        Assert.Equal(SyncRunStatus.Running, row.Status);
        Assert.Null(row.FailureReason);
    }

    [Fact]
    public async Task UpsertAsync_ExistingCheckpoint_UpdatesStatusAndReason()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        long runId = await CreateRunningRunAsync(scope.ServiceProvider, db);

        SyncCheckpointRepository sut =
            SyncTrackingPersistenceTestFixture.CreateCheckpointRepository(scope.ServiceProvider, db);

        await sut.UpsertAsync(runId,
                              " Dispatch ",
                              " Cursor-1 ",
                              SyncRunStatus.Running,
                              null,
                              CancellationToken.None);

        await sut.UpsertAsync(runId,
                              "Dispatch",
                              "Cursor-1",
                              SyncRunStatus.Failed,
                              "  failed reason  ",
                              CancellationToken.None);

        List<SyncCheckpointEntity> rows = await db.Set<SyncCheckpointEntity>()
                                                  .ToListAsync();
        Assert.Single(rows);

        SyncCheckpointEntity row = rows[0];
        Assert.Equal("Dispatch", row.Step);
        Assert.Equal("Cursor-1", row.Cursor);
        Assert.Equal(SyncRunStatus.Failed, row.Status);
        Assert.Equal("  failed reason  ", row.FailureReason);
    }

    [Fact]
    public async Task UpsertAsync_LongFailureReason_TruncatesTo1000()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        long runId = await CreateRunningRunAsync(scope.ServiceProvider, db);

        SyncCheckpointRepository sut =
            SyncTrackingPersistenceTestFixture.CreateCheckpointRepository(scope.ServiceProvider, db);

        string longReason = new string('X', 1200);

        await sut.UpsertAsync(runId,
                              "Dispatch",
                              "Cursor-2",
                              SyncRunStatus.Failed,
                              longReason,
                              CancellationToken.None);

        SyncCheckpointEntity row = await db.Set<SyncCheckpointEntity>()
                                           .SingleAsync();

        Assert.NotNull(row.FailureReason);
        Assert.Equal(1000, row.FailureReason!.Length);
    }

    [Fact]
    public async Task UpsertAsync_InvalidStep_ThrowsArgumentException()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        long runId = await CreateRunningRunAsync(scope.ServiceProvider, db);
        SyncCheckpointRepository sut =
            SyncTrackingPersistenceTestFixture.CreateCheckpointRepository(scope.ServiceProvider, db);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.UpsertAsync(runId,
                                                                          "   ",
                                                                          "cursor",
                                                                          SyncRunStatus.Running,
                                                                          null,
                                                                          CancellationToken.None));
    }

    [Fact]
    public async Task UpsertAsync_InvalidCursor_ThrowsArgumentException()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        long runId = await CreateRunningRunAsync(scope.ServiceProvider, db);
        SyncCheckpointRepository sut =
            SyncTrackingPersistenceTestFixture.CreateCheckpointRepository(scope.ServiceProvider, db);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.UpsertAsync(runId,
                                                                          "Dispatch",
                                                                          "   ",
                                                                          SyncRunStatus.Running,
                                                                          null,
                                                                          CancellationToken.None));
    }

    #endregion

    #region ========== *** GetLatestCompletedAsync *** ==========

    [Fact]
    public async Task GetLatestCompletedAsync_ReturnsNewestCompletedForStep()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        long runId = await CreateRunningRunAsync(scope.ServiceProvider, db);

        db.Set<SyncCheckpointEntity>()
          .AddRange(new SyncCheckpointEntity
                    {
                        RunId = runId,
                        Step = "Dispatch",
                        Cursor = "A",
                        Status = SyncRunStatus.Completed
                    },
                    new SyncCheckpointEntity
                    {
                        RunId = runId,
                        Step = "Dispatch",
                        Cursor = "B",
                        Status = SyncRunStatus.Failed
                    },
                    new SyncCheckpointEntity
                    {
                        RunId = runId,
                        Step = "Dispatch",
                        Cursor = "C",
                        Status = SyncRunStatus.Completed
                    });
        await db.SaveChangesAsync();

        SyncCheckpointRepository sut =
            SyncTrackingPersistenceTestFixture.CreateCheckpointRepository(scope.ServiceProvider, db);

        SyncCheckpointDto? dto = await sut.GetLatestCompletedAsync(runId, " Dispatch ", CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(SyncRunStatus.Completed, dto.Status);
        Assert.Equal("Dispatch", dto.Step);
    }

    [Fact]
    public async Task GetLatestCompletedAsync_InvalidStep_ThrowsArgumentException()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        long runId = await CreateRunningRunAsync(scope.ServiceProvider, db);
        SyncCheckpointRepository sut =
            SyncTrackingPersistenceTestFixture.CreateCheckpointRepository(scope.ServiceProvider, db);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.GetLatestCompletedAsync(runId,
                                                     "   ",
                                                     CancellationToken.None));
    }

    #endregion

    #region ========== *** GetFailedAsync *** ==========

    [Fact]
    public async Task GetFailedAsync_ReturnsOnlyFailedForRun()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        long runId1 = await CreateRunningRunAsync(scope.ServiceProvider, db);
        long runId2 = await CreateRunningRunAsync(scope.ServiceProvider, db);

        db.Set<SyncCheckpointEntity>()
          .AddRange(new SyncCheckpointEntity
                    {
                        RunId = runId1,
                        Step = "Dispatch",
                        Cursor = "1",
                        Status = SyncRunStatus.Failed
                    },
                    new SyncCheckpointEntity
                    {
                        RunId = runId1,
                        Step = "Dispatch",
                        Cursor = "2",
                        Status = SyncRunStatus.Completed
                    },
                    new SyncCheckpointEntity
                    {
                        RunId = runId2,
                        Step = "Dispatch",
                        Cursor = "3",
                        Status = SyncRunStatus.Failed
                    });
        await db.SaveChangesAsync();

        SyncCheckpointRepository sut =
            SyncTrackingPersistenceTestFixture.CreateCheckpointRepository(scope.ServiceProvider, db);

        IReadOnlyCollection<SyncCheckpointDto> failed = await sut.GetFailedAsync(runId1, CancellationToken.None);

        Assert.Single(failed);
        Assert.Equal(runId1,
                     failed.First()
                           .RunId);
        Assert.Equal(SyncRunStatus.Failed,
                     failed.First()
                           .Status);
    }

    #endregion

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private static async Task<long> CreateRunningRunAsync(IServiceProvider provider, AppDbContext db)
    {
        SyncRequestRepository requestRepository =
            SyncTrackingPersistenceTestFixture.CreateRequestRepository(provider, db);
        SyncRunRepository runRepository = SyncTrackingPersistenceTestFixture.CreateRunRepository(provider, db);

        SyncRequestEntity seed =
            SyncTrackingPersistenceTestFixture.CreateRequest(SyncCategory.UsersDetails,
                                                             SyncMode.Incremental,
                                                             $"i-{Guid.NewGuid()}");

        long requestId = await requestRepository.CreateOrGetByScopeAsync(seed.Category,
                                                                         seed.Mode,
                                                                         seed.Interval,
                                                                         seed.PageNumber,
                                                                         seed.GenesysJobId,
                                                                         CancellationToken.None);

        return await runRepository.StartNewRunAsync(requestId, CancellationToken.None);
    }

    #endregion
}
