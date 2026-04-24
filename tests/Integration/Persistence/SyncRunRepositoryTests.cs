using Application.Abstractions.Persistence;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;
using Infrastructure.Persistence.Repositories.SyncTracking;

using Microsoft.EntityFrameworkCore;

using Moq;

using SharedKernel.Time;

using tests.TestSupport.Persistence;
using tests.TestSupport.Time;

using Xunit;


namespace tests.Integration.Persistence;

public sealed class SyncRunRepositoryTests
{
    #region ========== *** StartNewRunAsync *** ==========

    [Fact]
    public async Task StartNewRunAsync_NoActiveRun_CreatesRunningRun_AndSetsCurrentRun()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunEntity>(dbContext);

        SyncRequestEntity request =
                        await SyncTrackingSeedFactory.SeedRequestAsync(dbContext,
                                                                       nameof(SyncReferenceCategory.Group),
                                                                       SyncMode.Full);

        SyncRunRepository sut = new SyncRunRepository(dbContext, uow.Object, dateTimeProvider.Object);

        long runId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);

        SyncRunEntity run = await dbContext.Set<SyncRunEntity>()
                                           .SingleAsync(x => x.Id == runId);
        SyncRequestEntity reloaded = await dbContext.Set<SyncRequestEntity>()
                                                    .SingleAsync(x => x.Id == request.Id);

        Assert.Equal(SyncRunStatus.Running, run.Status);
        Assert.Equal(1, run.AttemptNo);
        Assert.Equal(DateTimeProviderTestFactory.FixedNow, run.RunStartedAtEastern);
        Assert.Null(run.RunCompletedAtEastern);
        Assert.Equal(runId, reloaded.CurrentRunId);
        Assert.Equal(SyncRequestStatus.Running, reloaded.Status);
    }

    [Fact]
    public async Task StartNewRunAsync_WithActiveRun_SupersedesOldRun_AndIncrementsAttempt()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunEntity>(dbContext);

        SyncRequestEntity request =
                        await SyncTrackingSeedFactory.SeedRequestAsync(dbContext,
                                                                       nameof(SyncReferenceCategory.Skill),
                                                                       SyncMode.Full);

        SyncRunRepository sut = new SyncRunRepository(dbContext, uow.Object, dateTimeProvider.Object);

        long firstRunId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);
        long secondRunId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);

        SyncRunEntity firstRun = await dbContext.Set<SyncRunEntity>()
                                                .SingleAsync(x => x.Id == firstRunId);
        SyncRunEntity secondRun = await dbContext.Set<SyncRunEntity>()
                                                 .SingleAsync(x => x.Id == secondRunId);
        SyncRequestEntity reloaded = await dbContext.Set<SyncRequestEntity>()
                                                    .SingleAsync(x => x.Id == request.Id);

        Assert.Equal(SyncRunStatus.Superseded, firstRun.Status);
        Assert.Equal(DateTimeProviderTestFactory.FixedNow, firstRun.RunCompletedAtEastern);
        Assert.Equal(secondRunId, firstRun.SupersededByRunId);
        Assert.Equal(SyncRunStatus.Running, secondRun.Status);
        Assert.Equal(2, secondRun.AttemptNo);
        Assert.Equal(DateTimeProviderTestFactory.FixedNow, secondRun.RunStartedAtEastern);
        Assert.Equal(secondRunId, reloaded.CurrentRunId);
        Assert.Equal(SyncRequestStatus.Running, reloaded.Status);
    }

    #endregion

    [Fact]
    public async Task MarkCompletedWithRecoveryItemsAsync_MarksRunAndRequestWithRecoveryItems()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunEntity>(dbContext);

        SyncRequestEntity request =
                        await SyncTrackingSeedFactory.SeedRequestAsync(dbContext,
                                                                       nameof(SyncAnalyticsCategory.UsersDetails),
                                                                       SyncMode.Recovery);

        SyncRunRepository sut = new SyncRunRepository(dbContext, uow.Object, dateTimeProvider.Object);

        long runId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);

        await sut.MarkCompletedWithRecoveryItemsAsync(runId, CancellationToken.None);

        SyncRunEntity run = await dbContext.Set<SyncRunEntity>()
                                           .SingleAsync(x => x.Id == runId);
        SyncRequestEntity reloaded = await dbContext.Set<SyncRequestEntity>()
                                                    .SingleAsync(x => x.Id == request.Id);

        Assert.Equal(SyncRunStatus.CompletedWithRecoveryItems, run.Status);
        Assert.Equal(DateTimeProviderTestFactory.FixedNow, run.RunStartedAtEastern);
        Assert.Equal(DateTimeProviderTestFactory.FixedNow, run.RunCompletedAtEastern);
        Assert.Equal(SyncRequestStatus.CompletedWithRecoveryItems, reloaded.Status);
        Assert.Equal(runId, reloaded.CurrentRunId);
    }

    [Fact]
    public async Task IsCurrentRunAsync_ReturnsFalse_AfterRunCompleted()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunEntity>(dbContext);

        SyncRequestEntity request =
                        await SyncTrackingSeedFactory.SeedRequestAsync(dbContext,
                                                                       nameof(SyncReferenceCategory.WrapUpCode),
                                                                       SyncMode.Full);

        SyncRunRepository sut = new SyncRunRepository(dbContext, uow.Object, dateTimeProvider.Object);

        long runId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);

        bool before = await sut.IsCurrentRunAsync(runId, CancellationToken.None);
        await sut.MarkCompletedAsync(runId, CancellationToken.None);
        bool after = await sut.IsCurrentRunAsync(runId, CancellationToken.None);

        SyncRequestEntity reloaded = await dbContext.Set<SyncRequestEntity>()
                                                    .SingleAsync(x => x.Id == request.Id);

        Assert.True(before);
        Assert.False(after);
        Assert.Equal(SyncRequestStatus.Completed, reloaded.Status);
    }

    [Fact]
    public async Task MarkFailedAsync_StoresRunLevelSummary_AndMarksRequestFailed()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunEntity>(dbContext);

        SyncRequestEntity request =
                        await SyncTrackingSeedFactory.SeedRequestAsync(dbContext,
                                                                       nameof(SyncReferenceCategory.User),
                                                                       SyncMode.Full);

        SyncRunRepository sut = new SyncRunRepository(dbContext, uow.Object, dateTimeProvider.Object);

        long runId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);

        await sut.MarkFailedAsync(runId, "References full-sync for User is not wired yet.", CancellationToken.None);

        SyncRunEntity run = await dbContext.Set<SyncRunEntity>()
                                           .SingleAsync(x => x.Id == runId);
        SyncRequestEntity reloaded = await dbContext.Set<SyncRequestEntity>()
                                                    .SingleAsync(x => x.Id == request.Id);

        Assert.Equal(SyncRunStatus.Failed, run.Status);
        Assert.Equal("Requested sync category is not supported in the current release.", run.FailureReason);
        Assert.Equal(DateTimeProviderTestFactory.FixedNow, run.RunCompletedAtEastern);
        Assert.Equal(SyncRequestStatus.Failed, reloaded.Status);
    }

    [Fact]
    public async Task MarkCanceledAsync_NullReason_StoresDefaultRunSummary_AndMarksRequestCanceled()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunEntity>(dbContext);

        SyncRequestEntity request =
                        await SyncTrackingSeedFactory.SeedRequestAsync(dbContext,
                                                                       nameof(SyncAnalyticsCategory.UsersDetails),
                                                                       SyncMode.Recovery);

        SyncRunRepository sut = new SyncRunRepository(dbContext, uow.Object, dateTimeProvider.Object);

        long runId = await sut.StartNewRunAsync(request.Id, CancellationToken.None);

        await sut.MarkCanceledAsync(runId, null, CancellationToken.None);

        SyncRunEntity run = await dbContext.Set<SyncRunEntity>()
                                           .SingleAsync(x => x.Id == runId);
        SyncRequestEntity reloaded = await dbContext.Set<SyncRequestEntity>()
                                                    .SingleAsync(x => x.Id == request.Id);

        Assert.Equal(SyncRunStatus.Canceled, run.Status);
        Assert.Equal("Run was canceled.", run.FailureReason);
        Assert.Equal(DateTimeProviderTestFactory.FixedNow, run.RunCompletedAtEastern);
        Assert.Equal(SyncRequestStatus.Canceled, reloaded.Status);
    }
}
