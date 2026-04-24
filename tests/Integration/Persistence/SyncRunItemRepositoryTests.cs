using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
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

public sealed class SyncRunItemRepositoryTests
{
    #region ========== *** UpsertAsync *** ==========

    [Fact]
    public async Task UpsertAsync_NewRunItem_InsertsRow()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        SyncRunItemRepository sut = new SyncRunItemRepository(dbContext, uow.Object);

        await sut.UpsertAsync(runId,
                              "Dispatch",
                              "UsersDetails|Incremental|I1|-|-",
                              SyncRunStatus.Running,
                              null,
                              CancellationToken.None);

        SyncRunItemEntity row = await dbContext.Set<SyncRunItemEntity>()
                                               .SingleAsync();

        Assert.Equal(runId, row.RunId);
        Assert.Equal("Dispatch", row.Step);
        Assert.Equal("UsersDetails|Incremental|I1|-|-", row.Cursor);
        Assert.Equal(SyncRunStatus.Running, row.Status);
        Assert.Null(row.FailureReason);
    }

    [Fact]
    public async Task UpsertAsync_ExistingRunItem_UpdatesStatusAndReason()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        SyncRunItemRepository sut = new SyncRunItemRepository(dbContext, uow.Object);

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

        List<SyncRunItemEntity> rows = await dbContext.Set<SyncRunItemEntity>()
                                                      .ToListAsync();

        Assert.Single(rows);

        SyncRunItemEntity row = rows[0];
        Assert.Equal("Dispatch", row.Step);
        Assert.Equal("Cursor-1", row.Cursor);
        Assert.Equal(SyncRunStatus.Failed, row.Status);
        Assert.Equal("failed reason", row.FailureReason);
    }

    [Fact]
    public async Task UpsertAsync_LongFailureReason_TruncatesTo1000()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        SyncRunItemRepository sut = new SyncRunItemRepository(dbContext, uow.Object);

        string longReason = new string('X', 1200);

        await sut.UpsertAsync(runId,
                              "Dispatch",
                              "Cursor-2",
                              SyncRunStatus.Failed,
                              longReason,
                              CancellationToken.None);

        SyncRunItemEntity row = await dbContext.Set<SyncRunItemEntity>()
                                               .SingleAsync();

        Assert.NotNull(row.FailureReason);
        Assert.Equal(1000, row.FailureReason!.Length);
    }

    [Fact]
    public async Task UpsertAsync_InvalidStep_ThrowsArgumentException()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        SyncRunItemRepository sut = new SyncRunItemRepository(dbContext, uow.Object);

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
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        SyncRunItemRepository sut = new SyncRunItemRepository(dbContext, uow.Object);

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
    public async Task GetLatestCompletedAsync_ReturnsCompletedRunItemForStep()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        dbContext.Set<SyncRunItemEntity>()
                 .AddRange(new SyncRunItemEntity
                           {
                               RunId = runId,
                               Step = "Dispatch",
                               Cursor = "A",
                               Status = SyncRunStatus.Completed
                           },
                           new SyncRunItemEntity
                           {
                               RunId = runId,
                               Step = "Dispatch",
                               Cursor = "B",
                               Status = SyncRunStatus.Failed
                           });
        await dbContext.SaveChangesAsync();

        SyncRunItemRepository sut = new SyncRunItemRepository(dbContext, uow.Object);

        SyncRunItemDto? dto = await sut.GetLatestCompletedAsync(runId, " Dispatch ", CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(runId, dto.RunId);
        Assert.Equal("Dispatch", dto.Step);
        Assert.Equal(SyncRunStatus.Completed, dto.Status);
    }

    [Fact]
    public async Task GetLatestCompletedAsync_ReturnsCompletedWithRecoveryItemsForStep()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        dbContext.Set<SyncRunItemEntity>()
                 .Add(new SyncRunItemEntity
                      {
                          RunId = runId,
                          Step = "Dispatch",
                          Cursor = "A",
                          Status = SyncRunStatus.CompletedWithRecoveryItems
                      });
        await dbContext.SaveChangesAsync();

        SyncRunItemRepository sut = new SyncRunItemRepository(dbContext, uow.Object);

        SyncRunItemDto? dto = await sut.GetLatestCompletedAsync(runId, "Dispatch", CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(runId, dto.RunId);
        Assert.Equal("Dispatch", dto.Step);
        Assert.Equal(SyncRunStatus.CompletedWithRecoveryItems, dto.Status);
    }

    [Fact]
    public async Task GetLatestCompletedAsync_InvalidStep_ThrowsArgumentException()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        SyncRunItemRepository sut = new SyncRunItemRepository(dbContext, uow.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.GetLatestCompletedAsync(runId,
                                                        "   ",
                                                        CancellationToken.None));
    }

    #endregion

    #region ========== *** GetFailedAsync *** ==========

    [Fact]
    public async Task GetFailedAsync_ReturnsOnlyFailedForRun()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId1 = await CreateRunningRunAsync(dbContext);
        long runId2 = await CreateRunningRunAsync(dbContext);

        dbContext.Set<SyncRunItemEntity>()
                 .AddRange(new SyncRunItemEntity
                           {
                               RunId = runId1,
                               Step = "Dispatch",
                               Cursor = "1",
                               Status = SyncRunStatus.Failed
                           },
                           new SyncRunItemEntity
                           {
                               RunId = runId1,
                               Step = "Dispatch",
                               Cursor = "2",
                               Status = SyncRunStatus.Completed
                           },
                           new SyncRunItemEntity
                           {
                               RunId = runId2,
                               Step = "Dispatch",
                               Cursor = "3",
                               Status = SyncRunStatus.Failed
                           });
        await dbContext.SaveChangesAsync();

        SyncRunItemRepository sut = new SyncRunItemRepository(dbContext, uow.Object);

        IReadOnlyCollection<SyncRunItemDto> failed = await sut.GetFailedAsync(runId1, CancellationToken.None);

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

    private static async Task<long> CreateRunningRunAsync(AppDbContext dbContext)
    {
        SyncRequestEntity request =
                        await SyncTrackingSeedFactory.SeedRequestAsync(dbContext,
                                                                       nameof(SyncReferenceCategory.Group),
                                                                       SyncMode.Full);

        SyncRunEntity run = new SyncRunEntity
                            {
                                RequestId = request.Id,
                                Status = SyncRunStatus.Running,
                                AttemptNo = 1,
                                RunStartedAtEastern = DateTimeProviderTestFactory.FixedNow
                            };

        dbContext.Set<SyncRunEntity>()
                 .Add(run);
        await dbContext.SaveChangesAsync();

        return run.Id;
    }

    #endregion
}
