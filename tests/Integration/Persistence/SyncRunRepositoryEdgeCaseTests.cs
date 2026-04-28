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

public sealed class SyncRunRepositoryEdgeCaseTests
{
    [Fact]
    public async Task StartNewRunAsync_RequestNotFound_ThrowsInvalidOperationException()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunEntity>(dbContext);

        SyncRunRepository sut = new SyncRunRepository(dbContext, uow.Object, dateTimeProvider.Object);

        InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.StartNewRunAsync(9999L,
                                                                        CancellationToken.None));

        Assert.Contains("Sync request '9999' was not found.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MarkCompletedAsync_RunNotFound_ThrowsInvalidOperationException()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunEntity>(dbContext);

        SyncRunRepository sut = new SyncRunRepository(dbContext, uow.Object, dateTimeProvider.Object);

        InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.MarkCompletedAsync(7777L,
                                                                        CancellationToken.None));

        Assert.Contains("Sync run '7777' was not found.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MarkFailedAsync_WhenRunAlreadyCompleted_DoesNotOverwriteStatusOrReason()
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
        await sut.MarkCompletedAsync(runId, CancellationToken.None);

        SyncRunEntity before = await dbContext.Set<SyncRunEntity>()
                                              .AsNoTracking()
                                              .SingleAsync(x => x.Id == runId);

        await sut.MarkFailedAsync(runId, "should-not-apply", CancellationToken.None);

        SyncRunEntity after = await dbContext.Set<SyncRunEntity>()
                                             .AsNoTracking()
                                             .SingleAsync(x => x.Id == runId);

        Assert.Equal(SyncRunStatus.Completed, after.Status);
        Assert.Equal(before.RunCompletedAtEastern, after.RunCompletedAtEastern);
        Assert.Equal(before.FailureReason, after.FailureReason);
    }

    [Fact]
    public async Task MarkSupersededAsync_WhenRunAlreadyCanceled_DoesNotOverwriteFinalState()
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
        await sut.MarkCanceledAsync(runId, "cancelled", CancellationToken.None);

        SyncRunEntity before = await dbContext.Set<SyncRunEntity>()
                                              .AsNoTracking()
                                              .SingleAsync(x => x.Id == runId);

        await sut.MarkSupersededAsync(runId, 123456L, CancellationToken.None);

        SyncRunEntity after = await dbContext.Set<SyncRunEntity>()
                                             .AsNoTracking()
                                             .SingleAsync(x => x.Id == runId);

        Assert.Equal(SyncRunStatus.Canceled, after.Status);
        Assert.Equal(before.RunCompletedAtEastern, after.RunCompletedAtEastern);
        Assert.Equal(before.SupersededByRunId, after.SupersededByRunId);
        Assert.Equal(before.FailureReason, after.FailureReason);
    }
}
