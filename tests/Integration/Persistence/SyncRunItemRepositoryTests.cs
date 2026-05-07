using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;
using Infrastructure.Persistence.Repositories.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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
        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

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
        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

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
        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

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
        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

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
        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

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
        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
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

        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

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
        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
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

        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

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
        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

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
        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
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

        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

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

    [Fact]
    [Trait("Provider", "Sqlite")]
    public async Task SeedPendingPagesAsync_NewPages_InsertsPageRows()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateSqliteDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);

        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

        await sut.SeedPendingPagesAsync(runId,
                                        " Analytics:UsersDetails:PageFetch ",
                                        [1, 2, 2, 3],
                                        CancellationToken.None);

        List<SyncRunItemEntity> rows = await dbContext.Set<SyncRunItemEntity>()
                                                      .OrderBy(x => x.PageNumber)
                                                      .ToListAsync();

        Assert.Equal(3, rows.Count);
        Assert.All(rows,
                   row =>
                   {
                       Assert.Equal(runId, row.RunId);
                       Assert.Equal("Analytics:UsersDetails:PageFetch", row.Step);
                       Assert.Null(row.Cursor);
                       Assert.Equal(SyncRunStatus.Pending, row.Status);
                       Assert.Null(row.ClaimedBy);
                       Assert.Null(row.LeaseToken);
                       Assert.Equal(0, row.AttemptCount);
                   });

        int[] actualPageNumbers = rows.Select(x => x.PageNumber!.Value)
                                      .ToArray();

        Assert.Equal(new[] { 1, 2, 3 }, actualPageNumbers);
    }

    [Fact]
    [Trait("Provider", "Sqlite")]
    public async Task ClaimNextPageAsync_WhenPendingPagesExist_ClaimsLowestPage()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateSqliteDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);
        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

        await sut.SeedPendingPagesAsync(runId,
                                        "Analytics:UsersDetails:PageFetch",
                                        [2, 1],
                                        CancellationToken.None);

        Guid leaseToken = Guid.NewGuid();
        DateTimeOffset claimedAt = DateTimeProviderTestFactory.FixedNow;
        DateTimeOffset expiresAt = claimedAt.AddMinutes(5);

        SyncRunItemDto? claimed = await sut.ClaimNextPageAsync(runId,
                                                               "Analytics:UsersDetails:PageFetch",
                                                               "worker-a",
                                                               leaseToken,
                                                               claimedAt,
                                                               expiresAt,
                                                               CancellationToken.None);

        Assert.NotNull(claimed);
        Assert.Equal(1, claimed.PageNumber);
        Assert.Equal(SyncRunStatus.Running, claimed.Status);
        Assert.Equal("worker-a", claimed.ClaimedBy);
        Assert.Equal(leaseToken, claimed.LeaseToken);
        Assert.Equal(claimedAt, claimed.ClaimedAtEastern);
        Assert.Equal(expiresAt, claimed.ClaimExpiresAtEastern);
        Assert.Equal(1, claimed.AttemptCount);
        Assert.Equal(claimedAt, claimed.LastHeartbeatAtEastern);
    }

    [Fact]
    [Trait("Provider", "Sqlite")]
    public async Task ClaimNextPageAsync_WhenLeaseNotExpired_DoesNotClaimRunningPage()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateSqliteDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);
        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

        await sut.SeedPendingPagesAsync(runId,
                                        "Analytics:UsersDetails:PageFetch",
                                        [1],
                                        CancellationToken.None);

        DateTimeOffset claimedAt = DateTimeProviderTestFactory.FixedNow;

        SyncRunItemDto? firstClaim = await sut.ClaimNextPageAsync(runId,
                                                                  "Analytics:UsersDetails:PageFetch",
                                                                  "worker-a",
                                                                  Guid.NewGuid(),
                                                                  claimedAt,
                                                                  claimedAt.AddMinutes(5),
                                                                  CancellationToken.None);

        SyncRunItemDto? secondClaim = await sut.ClaimNextPageAsync(runId,
                                                                   "Analytics:UsersDetails:PageFetch",
                                                                   "worker-b",
                                                                   Guid.NewGuid(),
                                                                   claimedAt.AddMinutes(1),
                                                                   claimedAt.AddMinutes(6),
                                                                   CancellationToken.None);

        Assert.NotNull(firstClaim);
        Assert.Null(secondClaim);
    }

    [Fact]
    [Trait("Provider", "Sqlite")]
    public async Task ClaimNextPageAsync_WhenLeaseExpired_ReclaimsRunningPage()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateSqliteDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);
        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

        await sut.SeedPendingPagesAsync(runId,
                                        "Analytics:UsersDetails:PageFetch",
                                        [1],
                                        CancellationToken.None);

        DateTimeOffset firstClaimedAt = DateTimeProviderTestFactory.FixedNow;

        SyncRunItemDto? firstClaim = await sut.ClaimNextPageAsync(runId,
                                                                  "Analytics:UsersDetails:PageFetch",
                                                                  "worker-a",
                                                                  Guid.NewGuid(),
                                                                  firstClaimedAt,
                                                                  firstClaimedAt.AddMinutes(5),
                                                                  CancellationToken.None);

        Guid secondLeaseToken = Guid.NewGuid();
        DateTimeOffset secondClaimedAt = firstClaimedAt.AddMinutes(6);

        SyncRunItemDto? secondClaim = await sut.ClaimNextPageAsync(runId,
                                                                   "Analytics:UsersDetails:PageFetch",
                                                                   "worker-b",
                                                                   secondLeaseToken,
                                                                   secondClaimedAt,
                                                                   secondClaimedAt.AddMinutes(5),
                                                                   CancellationToken.None);

        Assert.NotNull(firstClaim);
        Assert.NotNull(secondClaim);
        Assert.Equal(firstClaim.Id, secondClaim.Id);
        Assert.Equal("worker-b", secondClaim.ClaimedBy);
        Assert.Equal(secondLeaseToken, secondClaim.LeaseToken);
        Assert.Equal(2, secondClaim.AttemptCount);
    }

    [Fact]
    [Trait("Provider", "Sqlite")]
    public async Task TryHeartbeatAsync_WhenOwnedLease_ExtendsLease()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateSqliteDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);
        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

        await sut.SeedPendingPagesAsync(runId,
                                        "Analytics:UsersDetails:PageFetch",
                                        [1],
                                        CancellationToken.None);

        Guid leaseToken = Guid.NewGuid();
        DateTimeOffset claimedAt = DateTimeProviderTestFactory.FixedNow;

        SyncRunItemDto claimed = await sut.ClaimNextPageAsync(runId,
                                                              "Analytics:UsersDetails:PageFetch",
                                                              "worker-a",
                                                              leaseToken,
                                                              claimedAt,
                                                              claimedAt.AddMinutes(5),
                                                              CancellationToken.None)
                                 ?? throw new InvalidOperationException("Expected a claimed page.");

        DateTimeOffset heartbeatAt = claimedAt.AddMinutes(2);
        DateTimeOffset newExpiry = heartbeatAt.AddMinutes(5);

        bool result = await sut.TryHeartbeatAsync(claimed.Id,
                                                  "worker-a",
                                                  leaseToken,
                                                  heartbeatAt,
                                                  newExpiry,
                                                  CancellationToken.None);

        SyncRunItemEntity row = await dbContext.Set<SyncRunItemEntity>()
                                               .AsNoTracking()
                                               .SingleAsync(x => x.Id == claimed.Id);

        Assert.True(result);
        Assert.Equal(heartbeatAt, row.LastHeartbeatAtEastern);
        Assert.Equal(newExpiry, row.ClaimExpiresAtEastern);
    }

    [Fact]
    [Trait("Provider", "Sqlite")]
    public async Task TryMarkCompletedAsync_WhenOwnedLease_CompletesAndClearsLease()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateSqliteDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);
        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

        await sut.SeedPendingPagesAsync(runId,
                                        "Analytics:UsersDetails:PageFetch",
                                        [1],
                                        CancellationToken.None);

        Guid leaseToken = Guid.NewGuid();
        DateTimeOffset claimedAt = DateTimeProviderTestFactory.FixedNow;

        SyncRunItemDto claimed = await sut.ClaimNextPageAsync(runId,
                                                              "Analytics:UsersDetails:PageFetch",
                                                              "worker-a",
                                                              leaseToken,
                                                              claimedAt,
                                                              claimedAt.AddMinutes(5),
                                                              CancellationToken.None)
                                 ?? throw new InvalidOperationException("Expected a claimed page.");

        bool result = await sut.TryMarkCompletedAsync(claimed.Id,
                                                      "worker-a",
                                                      leaseToken,
                                                      CancellationToken.None);

        SyncRunItemEntity row = await dbContext.Set<SyncRunItemEntity>()
                                               .AsNoTracking()
                                               .SingleAsync(x => x.Id == claimed.Id);

        Assert.True(result);
        Assert.Equal(SyncRunStatus.Completed, row.Status);
        Assert.Null(row.FailureReason);
        Assert.Null(row.ClaimedBy);
        Assert.Null(row.LeaseToken);
        Assert.Null(row.ClaimedAtEastern);
        Assert.Null(row.ClaimExpiresAtEastern);
        Assert.Null(row.LastHeartbeatAtEastern);
    }

    [Fact]
    [Trait("Provider", "Sqlite")]
    public async Task TryMarkFailedAsync_WhenOwnedLease_FailsAndClearsLease()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateSqliteDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);
        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

        await sut.SeedPendingPagesAsync(runId,
                                        "Analytics:UsersDetails:PageFetch",
                                        [1],
                                        CancellationToken.None);

        Guid leaseToken = Guid.NewGuid();
        DateTimeOffset claimedAt = DateTimeProviderTestFactory.FixedNow;

        SyncRunItemDto claimed = await sut.ClaimNextPageAsync(runId,
                                                              "Analytics:UsersDetails:PageFetch",
                                                              "worker-a",
                                                              leaseToken,
                                                              claimedAt,
                                                              claimedAt.AddMinutes(5),
                                                              CancellationToken.None)
                                 ?? throw new InvalidOperationException("Expected a claimed page.");

        bool result = await sut.TryMarkFailedAsync(claimed.Id,
                                                   "worker-a",
                                                   leaseToken,
                                                   " failed ",
                                                   CancellationToken.None);

        SyncRunItemEntity row = await dbContext.Set<SyncRunItemEntity>()
                                               .AsNoTracking()
                                               .SingleAsync(x => x.Id == claimed.Id);

        Assert.True(result);
        Assert.Equal(SyncRunStatus.Failed, row.Status);
        Assert.Equal("failed", row.FailureReason);
        Assert.Null(row.ClaimedBy);
        Assert.Null(row.LeaseToken);
        Assert.Null(row.ClaimedAtEastern);
        Assert.Null(row.ClaimExpiresAtEastern);
        Assert.Null(row.LastHeartbeatAtEastern);
    }

    [Fact]
    [Trait("Provider", "Sqlite")]
    public async Task GetFailedPagesAsync_ReturnsOnlyFailedPagesForStep()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateSqliteDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);
        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

        await sut.SeedPendingPagesAsync(runId,
                                        "Analytics:UsersDetails:PageFetch",
                                        [1, 2],
                                        CancellationToken.None);

        dbContext.Set<SyncRunItemEntity>()
                 .Add(new SyncRunItemEntity
                      {
                          RunId = runId,
                          Step = "Dispatch",
                          Cursor = "generic",
                          Status = SyncRunStatus.Failed
                      });
        await dbContext.SaveChangesAsync();

        SyncRunItemDto claimed = await sut.ClaimNextPageAsync(runId,
                                                              "Analytics:UsersDetails:PageFetch",
                                                              "worker-a",
                                                              Guid.NewGuid(),
                                                              DateTimeProviderTestFactory.FixedNow,
                                                              DateTimeProviderTestFactory.FixedNow.AddMinutes(5),
                                                              CancellationToken.None)
                                 ?? throw new InvalidOperationException("Expected a claimed page.");

        await sut.TryMarkFailedAsync(claimed.Id,
                                     "worker-a",
                                     claimed.LeaseToken!.Value,
                                     "page failed",
                                     CancellationToken.None);

        IReadOnlyCollection<SyncRunItemDto> failedPages =
                await sut.GetFailedPagesAsync(runId, " Analytics:UsersDetails:PageFetch ", CancellationToken.None);

        SyncRunItemDto failedPage = Assert.Single(failedPages);
        Assert.Equal(1, failedPage.PageNumber);
        Assert.Equal(SyncRunStatus.Failed, failedPage.Status);
        Assert.Equal("page failed", failedPage.FailureReason);
    }

    [Fact]
    [Trait("Provider", "Sqlite")]
    public async Task HasUnfinishedPagesAsync_WhenPendingOrRunningPagesExist_ReturnsTrue()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();
        await using AppDbContext dbContext = PersistenceTestFactory.CreateSqliteDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRunItemEntity>(dbContext);

        long runId = await CreateRunningRunAsync(dbContext);
        SyncRunItemRepository sut = CreateRepository(dbContext, uow.Object);

        await sut.SeedPendingPagesAsync(runId,
                                        "Analytics:UsersDetails:PageFetch",
                                        [1],
                                        CancellationToken.None);

        bool result = await sut.HasUnfinishedPagesAsync(runId,
                                                        "Analytics:UsersDetails:PageFetch",
                                                        CancellationToken.None);

        Assert.True(result);
    }

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

    private static SyncRunItemRepository CreateRepository(AppDbContext dbContext, IUnitOfWork uow)
    {
        return new SyncRunItemRepository(dbContext, uow, NullLogger<SyncRunItemRepository>.Instance);
    }

    #endregion
}
