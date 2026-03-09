using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;
using Infrastructure.Persistence.Repositories.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using tests.TestSupport.Persistence;
using tests.TestSupport.Time;

using Xunit;


namespace Tests.Integration.Persistence;

public sealed class SyncRunRepositoryTests
{
    [Fact]
    public async Task StartNewRunAsync_NoActiveRun_CreatesRunningRun_AndSetsCurrentRun()
    {
        await using ServiceProvider provider =
            SyncRunRepositoryTestFixture.BuildProvider(new FixedEstDateTimeProvider());
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRequestEntity request =
            SyncRunRepositoryTestFixture.CreateRequest(SyncCategory.UsersDetails, SyncMode.Incremental, "i-1");
        db.Set<SyncRequestEntity>()
          .Add(request);
        await db.SaveChangesAsync();

        SyncRunRepository sut = SyncRunRepositoryTestFixture.CreateSut(scope.ServiceProvider, db);

        long runId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);

        SyncRunEntity run = await db.Set<SyncRunEntity>()
                                    .SingleAsync(x => x.Id == runId);
        SyncRequestEntity reloaded = await db.Set<SyncRequestEntity>()
                                             .SingleAsync(x => x.Id == request.Id);

        Assert.Equal(SyncRunStatus.Running, run.Status);
        Assert.Equal(1, run.AttemptNo);
        Assert.NotNull(run.RunStartedAt);
        Assert.Null(run.RunCompletedAt);
        Assert.Equal(runId, reloaded.CurrentRunId);
    }

    [Fact]
    public async Task StartNewRunAsync_WithActiveRun_SupersedesOldRun_AndIncrementsAttempt()
    {
        await using ServiceProvider provider =
            SyncRunRepositoryTestFixture.BuildProvider(new FixedEstDateTimeProvider());
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRequestEntity request =
            SyncRunRepositoryTestFixture.CreateRequest(SyncCategory.ConversationsDetails, SyncMode.Incremental, "i-2");
        db.Set<SyncRequestEntity>()
          .Add(request);
        await db.SaveChangesAsync();

        SyncRunRepository sut = SyncRunRepositoryTestFixture.CreateSut(scope.ServiceProvider, db);

        long firstRunId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);
        long secondRunId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);

        SyncRunEntity firstRun = await db.Set<SyncRunEntity>()
                                         .SingleAsync(x => x.Id == firstRunId);
        SyncRunEntity secondRun = await db.Set<SyncRunEntity>()
                                          .SingleAsync(x => x.Id == secondRunId);
        SyncRequestEntity reloaded = await db.Set<SyncRequestEntity>()
                                             .SingleAsync(x => x.Id == request.Id);

        Assert.Equal(SyncRunStatus.Superseded, firstRun.Status);
        Assert.NotNull(firstRun.RunCompletedAt);
        Assert.Equal(secondRunId, firstRun.SupersededByRunId);

        Assert.Equal(SyncRunStatus.Running, secondRun.Status);
        Assert.Equal(2, secondRun.AttemptNo);
        Assert.NotNull(secondRun.RunStartedAt);

        Assert.Equal(secondRunId, reloaded.CurrentRunId);
    }

    [Fact]
    public async Task IsCurrentRunAsync_ReturnsFalse_AfterRunIsCompleted()
    {
        await using ServiceProvider provider =
            SyncRunRepositoryTestFixture.BuildProvider(new FixedEstDateTimeProvider());
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRequestEntity request =
            SyncRunRepositoryTestFixture.CreateRequest(SyncCategory.UsersDetails, SyncMode.Incremental, "i-3");
        db.Set<SyncRequestEntity>()
          .Add(request);
        await db.SaveChangesAsync();

        SyncRunRepository sut = SyncRunRepositoryTestFixture.CreateSut(scope.ServiceProvider, db);

        long runId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);

        bool isCurrentBefore = await sut.IsCurrentRunAsync(runId, CancellationToken.None);
        await sut.MarkCompletedAsync(runId, CancellationToken.None);
        bool isCurrentAfter = await sut.IsCurrentRunAsync(runId, CancellationToken.None);

        SyncRunEntity run = await db.Set<SyncRunEntity>()
                                    .SingleAsync(x => x.Id == runId);

        Assert.True(isCurrentBefore);
        Assert.False(isCurrentAfter);
        Assert.Equal(SyncRunStatus.Completed, run.Status);
        Assert.NotNull(run.RunCompletedAt);
    }

    [Fact]
    public async Task MarkCanceledAsync_NullReason_UsesDefaultMessage()
    {
        await using ServiceProvider provider =
            SyncRunRepositoryTestFixture.BuildProvider(new FixedEstDateTimeProvider());
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRequestEntity request =
            SyncRunRepositoryTestFixture.CreateRequest(SyncCategory.UsersDetails, SyncMode.Recovery, "i-4");
        db.Set<SyncRequestEntity>()
          .Add(request);
        await db.SaveChangesAsync();

        SyncRunRepository sut = SyncRunRepositoryTestFixture.CreateSut(scope.ServiceProvider, db);

        long runId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);

        await sut.MarkCanceledAsync(runId, null, CancellationToken.None);

        SyncRunEntity run = await db.Set<SyncRunEntity>()
                                    .SingleAsync(x => x.Id == runId);

        Assert.Equal(SyncRunStatus.Canceled, run.Status);
        Assert.Equal("Canceled by host/user.", run.FailureReason);
        Assert.NotNull(run.RunCompletedAt);
    }
}
