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

public sealed class SyncRequestRepositoryTests
{
    #region ========== *** CreateOrGetByScopeAsync *** ==========

    #region ========== ** Full ** ==========

    [Fact]
    public async Task CreateOrGetByScopeAsync_WhenFullScopeAlreadyExists_ReturnsExistingResult_AndSkipsUnitOfWork()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);

        SyncRequestEntity existing = BuildRequest(nameof(SyncReferenceCategory.Group),
                                                  SyncMode.Full,
                                                  SyncRequestStatus.Completed,
                                                  null,
                                                  null,
                                                  null);

        dbContext.Set<SyncRequestEntity>()
                 .Add(existing);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        SyncRequestResolveResult result = await sut.CreateOrGetByScopeAsync(nameof(SyncReferenceCategory.Group),
                                                                            SyncMode.Full,
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            CancellationToken.None);

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(existing.PublicId, result.PublicId);
        Assert.Equal(SyncRequestResolveAction.ReusedActive, result.RequestAction);

        uow.Verify(x => x.UpsertAsync(It.IsAny<SyncRequestEntity>(),
                                      It.IsAny<Action<SyncRequestEntity>?>(),
                                      It.IsAny<CancellationToken>()),
                   Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region ========== ** Incremental ** ==========

    [Fact]
    public async Task
            CreateOrGetByScopeAsync_WhenIncrementalScopeAlreadyExists_ReturnsExistingResult_AndSkipsUnitOfWork()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);

        SyncRequestEntity existing = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                  SyncMode.Incremental,
                                                  SyncRequestStatus.Completed,
                                                  null,
                                                  null,
                                                  null);

        dbContext.Set<SyncRequestEntity>()
                 .Add(existing);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        SyncRequestResolveResult result = await sut.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                            SyncMode.Incremental,
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            CancellationToken.None);

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(existing.PublicId, result.PublicId);
        Assert.Equal(SyncRequestResolveAction.ReusedActive, result.RequestAction);

        uow.Verify(x => x.UpsertAsync(It.IsAny<SyncRequestEntity>(),
                                      It.IsAny<Action<SyncRequestEntity>?>(),
                                      It.IsAny<CancellationToken>()),
                   Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrGetByScopeAsync_WhenIncrementalScopeDoesNotExist_CreatesAndReturnsCreatedResult()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateMockUnitOfWork<SyncRequestEntity>(dbContext);

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        SyncRequestResolveResult result = await sut.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                            SyncMode.Incremental,
                                                                            "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z",
                                                                            1,
                                                                            null,
                                                                            CancellationToken.None);

        SyncRequestEntity created = await dbContext.Set<SyncRequestEntity>()
                                                   .SingleAsync();

        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.PublicId, result.PublicId);
        Assert.Equal(SyncRequestResolveAction.Created, result.RequestAction);
        Assert.Equal(nameof(SyncAnalyticsCategory.UsersDetails), created.Category);
        Assert.Equal(SyncMode.Incremental, created.Mode);
        Assert.Equal(SyncRequestStatus.Pending, created.Status);
        Assert.Equal(0, created.ReopenCount);
        Assert.Equal("2026-04-14T00:00:00Z/2026-04-14T00:30:00Z", created.Interval);
        Assert.Equal(1, created.PageNumber);
        Assert.Null(created.GenesysJobId);
        Assert.False(string.IsNullOrWhiteSpace(created.ScopeKey));

        uow.Verify(x => x.UpsertAsync(It.IsAny<SyncRequestEntity>(), null, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ========== ** Recovery ** ==========

    [Fact]
    public async Task CreateOrGetByScopeAsync_WhenRecoveryLatestIsActive_ReturnsExistingActiveResult()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);

        SyncRequestEntity active = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                SyncMode.Recovery,
                                                SyncRequestStatus.Running,
                                                "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z",
                                                1,
                                                null);

        dbContext.Set<SyncRequestEntity>()
                 .Add(active);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        SyncRequestResolveResult result = await sut.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                            SyncMode.Recovery,
                                                                            "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z",
                                                                            1,
                                                                            null,
                                                                            CancellationToken.None);

        Assert.Equal(active.Id, result.Id);
        Assert.Equal(active.PublicId, result.PublicId);
        Assert.Equal(SyncRequestResolveAction.ReusedActive, result.RequestAction);

        uow.Verify(x => x.UpsertAsync(It.IsAny<SyncRequestEntity>(),
                                      It.IsAny<Action<SyncRequestEntity>?>(),
                                      It.IsAny<CancellationToken>()),
                   Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(SyncRequestStatus.Failed)]
    [InlineData(SyncRequestStatus.Canceled)]
    public async Task CreateOrGetByScopeAsync_WhenRecoveryLatestIsReusable_ReopensExistingRow(SyncRequestStatus status)
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);

        SyncRequestEntity reusable = BuildRequest(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                  SyncMode.Recovery,
                                                  status,
                                                  "2026-04-14T01:00:00Z/2026-04-14T01:30:00Z",
                                                  2,
                                                  null);
        reusable.CurrentRunId = 123;
        reusable.ReopenCount = 4;

        dbContext.Set<SyncRequestEntity>()
                 .Add(reusable);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns<CancellationToken>(dbContext.SaveChangesAsync);

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        SyncRequestResolveResult result =
                await sut.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                  SyncMode.Recovery,
                                                  "2026-04-14T01:00:00Z/2026-04-14T01:30:00Z",
                                                  2,
                                                  null,
                                                  CancellationToken.None);

        Assert.Equal(reusable.Id, result.Id);
        Assert.Equal(reusable.PublicId, result.PublicId);
        Assert.Equal(SyncRequestResolveAction.ReusedFailed, result.RequestAction);
        Assert.Equal(SyncRequestStatus.Pending, reusable.Status);
        Assert.Null(reusable.CurrentRunId);
        Assert.Equal(5, reusable.ReopenCount);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.UpsertAsync(It.IsAny<SyncRequestEntity>(),
                                      It.IsAny<Action<SyncRequestEntity>?>(),
                                      It.IsAny<CancellationToken>()),
                   Times.Never);
    }

    [Fact]
    public async Task CreateOrGetByScopeAsync_WhenRecoveryLatestIsCompleted_CreatesNewRow()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateMockUnitOfWork<SyncRequestEntity>(dbContext);

        SyncRequestEntity completed = BuildRequest(nameof(SyncAnalyticsCategory.ConversationsAggregates),
                                                   SyncMode.Recovery,
                                                   SyncRequestStatus.Completed,
                                                   "2026-04-14T02:00:00Z/2026-04-14T02:30:00Z",
                                                   null,
                                                   null);

        dbContext.Set<SyncRequestEntity>()
                 .Add(completed);
        await dbContext.SaveChangesAsync();

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        SyncRequestResolveResult result =
                await sut.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsAggregates),
                                                  SyncMode.Recovery,
                                                  "2026-04-14T02:00:00Z/2026-04-14T02:30:00Z",
                                                  null,
                                                  null,
                                                  CancellationToken.None);

        List<SyncRequestEntity> rows = await dbContext.Set<SyncRequestEntity>()
                                                      .OrderBy(x => x.Id)
                                                      .ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal(completed.Id, rows[0].Id);
        Assert.Equal(rows[1].Id, result.Id);
        Assert.Equal(rows[1].PublicId, result.PublicId);
        Assert.Equal(SyncRequestResolveAction.Created, result.RequestAction);
        Assert.Equal(SyncRequestStatus.Pending, rows[1].Status);
        Assert.Equal(0, rows[1].ReopenCount);
    }

    [Fact]
    public async Task CreateOrGetByScopeAsync_WhenRecoveryLatestIsCompletedWithRecoveryItems_CreatesNewRow()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateMockUnitOfWork<SyncRequestEntity>(dbContext);

        SyncRequestEntity completed = BuildRequest(nameof(SyncAnalyticsCategory.ConversationsAggregates),
                                                   SyncMode.Recovery,
                                                   SyncRequestStatus.CompletedWithRecoveryItems,
                                                   "2026-04-14T02:00:00Z/2026-04-14T02:30:00Z",
                                                   null,
                                                   null);

        dbContext.Set<SyncRequestEntity>()
                 .Add(completed);
        await dbContext.SaveChangesAsync();

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        SyncRequestResolveResult result =
                await sut.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsAggregates),
                                                  SyncMode.Recovery,
                                                  "2026-04-14T02:00:00Z/2026-04-14T02:30:00Z",
                                                  null,
                                                  null,
                                                  CancellationToken.None);

        List<SyncRequestEntity> rows = await dbContext.Set<SyncRequestEntity>()
                                                      .OrderBy(x => x.Id)
                                                      .ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal(completed.Id, rows[0].Id);
        Assert.Equal(rows[1].Id, result.Id);
        Assert.Equal(rows[1].PublicId, result.PublicId);
        Assert.Equal(SyncRequestResolveAction.Created, result.RequestAction);
        Assert.Equal(SyncRequestStatus.Pending, rows[1].Status);
        Assert.Equal(0, rows[1].ReopenCount);
    }

    [Fact]
    public async Task CreateOrGetByScopeAsync_WhenRecoveryRowsTieOnUpdatedAt_UsesHighestIdAsLatest()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateMockUnitOfWork<SyncRequestEntity>(dbContext);

        const string interval = "2026-04-14T03:00:00Z/2026-04-14T03:30:00Z";

        SyncRequestEntity olderFailed = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                     SyncMode.Recovery,
                                                     SyncRequestStatus.Failed,
                                                     interval,
                                                     null,
                                                     null);

        dbContext.Set<SyncRequestEntity>()
                 .Add(olderFailed);
        await dbContext.SaveChangesAsync();

        SyncRequestEntity latestCompleted = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                         SyncMode.Recovery,
                                                         SyncRequestStatus.Completed,
                                                         interval,
                                                         null,
                                                         null);

        dbContext.Set<SyncRequestEntity>()
                 .Add(latestCompleted);
        await dbContext.SaveChangesAsync();

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        SyncRequestResolveResult result = await sut.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                            SyncMode.Recovery,
                                                                            interval,
                                                                            null,
                                                                            null,
                                                                            CancellationToken.None);

        List<SyncRequestEntity> rows = await dbContext.Set<SyncRequestEntity>()
                                                      .OrderBy(x => x.Id)
                                                      .ToListAsync();

        Assert.Equal(3, rows.Count);
        Assert.Equal(SyncRequestStatus.Failed, olderFailed.Status);
        Assert.Equal(SyncRequestStatus.Completed, latestCompleted.Status);
        Assert.Equal(rows[2].Id, result.Id);
        Assert.Equal(SyncRequestResolveAction.Created, result.RequestAction);
    }

    #endregion

    #endregion

    #region ========== *** GetByIdAsync *** ==========

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsProjectedDto()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);

        SyncRequestEntity entity = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                SyncMode.Recovery,
                                                SyncRequestStatus.Failed,
                                                null,
                                                3,
                                                "JOB-123");
        entity.CurrentRunId = 99;
        entity.ReopenCount = 2;

        dbContext.Set<SyncRequestEntity>()
                 .Add(entity);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        SyncRequestDto? dto = await sut.GetByIdAsync(entity.Id, CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.PublicId, dto.PublicId);
        Assert.Equal(entity.Category, dto.Category);
        Assert.Equal(entity.Mode, dto.Mode);
        Assert.Equal(entity.Status, dto.Status);
        Assert.Equal(entity.ReopenCount, dto.ReopenCount);
        Assert.Equal(entity.Interval, dto.Interval);
        Assert.Equal(entity.PageNumber, dto.PageNumber);
        Assert.Equal(entity.GenesysJobId, dto.GenesysJobId);
        Assert.Equal(entity.ScopeKey, dto.ScopeKey);
        Assert.Equal(entity.CurrentRunId, dto.CurrentRunId);
    }

    #endregion

    #region ========== *** GetEligibleRecoveryRequestsAsync *** ==========

    [Fact]
    public async Task GetEligibleRecoveryRequestsAsync_ReturnsPendingAndRetryableRecoveryRows()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);

        SyncRequestEntity pending = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                 SyncMode.Recovery,
                                                 SyncRequestStatus.Pending,
                                                 "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z",
                                                 1,
                                                 null);

        SyncRequestEntity failedRetryable = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                         SyncMode.Recovery,
                                                         SyncRequestStatus.Failed,
                                                         "2026-04-14T00:30:00Z/2026-04-14T01:00:00Z",
                                                         2,
                                                         null);
        failedRetryable.ReopenCount = 3;

        SyncRequestEntity canceledRetryable = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                           SyncMode.Recovery,
                                                           SyncRequestStatus.Canceled,
                                                           "2026-04-14T01:00:00Z/2026-04-14T01:30:00Z",
                                                           3,
                                                           null);
        canceledRetryable.ReopenCount = 2;

        dbContext.Set<SyncRequestEntity>()
                 .AddRange(pending, failedRetryable, canceledRetryable);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        IReadOnlyCollection<SyncRequestDto> rows =
                await sut.GetEligibleRecoveryRequestsAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                           CancellationToken.None);

        Assert.Equal(3, rows.Count);
        Assert.Contains(rows, x => x.Id == pending.Id           && x.Status == SyncRequestStatus.Pending);
        Assert.Contains(rows, x => x.Id == failedRetryable.Id   && x.Status == SyncRequestStatus.Failed);
        Assert.Contains(rows, x => x.Id == canceledRetryable.Id && x.Status == SyncRequestStatus.Canceled);
    }

    [Fact]
    public async Task GetEligibleRecoveryRequestsAsync_ExcludesRunningTerminalAndOverBudgetRows()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);

        SyncRequestEntity running = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                 SyncMode.Recovery,
                                                 SyncRequestStatus.Running,
                                                 "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z",
                                                 null,
                                                 null);

        SyncRequestEntity completed = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                   SyncMode.Recovery,
                                                   SyncRequestStatus.Completed,
                                                   "2026-04-14T00:30:00Z/2026-04-14T01:00:00Z",
                                                   null,
                                                   null);

        SyncRequestEntity completedWithRecoveryItems = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                    SyncMode.Recovery,
                                                                    SyncRequestStatus.CompletedWithRecoveryItems,
                                                                    "2026-04-14T01:00:00Z/2026-04-14T01:30:00Z",
                                                                    null,
                                                                    null);

        SyncRequestEntity failedOverBudget = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                          SyncMode.Recovery,
                                                          SyncRequestStatus.Failed,
                                                          "2026-04-14T01:30:00Z/2026-04-14T02:00:00Z",
                                                          null,
                                                          null);
        failedOverBudget.ReopenCount = 4;

        SyncRequestEntity otherCategoryPending = BuildRequest(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                              SyncMode.Recovery,
                                                              SyncRequestStatus.Pending,
                                                              "2026-04-14T02:00:00Z/2026-04-14T02:30:00Z",
                                                              null,
                                                              null);

        SyncRequestEntity otherModePending = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                          SyncMode.Incremental,
                                                          SyncRequestStatus.Pending,
                                                          "2026-04-14T02:30:00Z/2026-04-14T03:00:00Z",
                                                          null,
                                                          null);

        dbContext.Set<SyncRequestEntity>()
                 .AddRange(running,
                           completed,
                           completedWithRecoveryItems,
                           failedOverBudget,
                           otherCategoryPending,
                           otherModePending);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        IReadOnlyCollection<SyncRequestDto> rows =
                await sut.GetEligibleRecoveryRequestsAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                           CancellationToken.None);

        Assert.Empty(rows);
    }

    [Fact]
    public async Task GetEligibleRecoveryRequestsAsync_OrdersPendingFirstThenByIntervalAndPage()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);

        SyncRequestEntity pendingNewestInterval = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                               SyncMode.Recovery,
                                                               SyncRequestStatus.Pending,
                                                               "2026-04-14T02:00:00Z/2026-04-14T02:30:00Z",
                                                               2,
                                                               null);

        SyncRequestEntity pendingOldestInterval = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                               SyncMode.Recovery,
                                                               SyncRequestStatus.Pending,
                                                               "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z",
                                                               3,
                                                               null);

        SyncRequestEntity failedOldestInterval = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                              SyncMode.Recovery,
                                                              SyncRequestStatus.Failed,
                                                              "2026-04-14T00:30:00Z/2026-04-14T01:00:00Z",
                                                              5,
                                                              null);
        failedOldestInterval.ReopenCount = 1;

        SyncRequestEntity canceledSameIntervalLowerPage = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                       SyncMode.Recovery,
                                                                       SyncRequestStatus.Canceled,
                                                                       "2026-04-14T01:30:00Z/2026-04-14T02:00:00Z",
                                                                       1,
                                                                       null);
        canceledSameIntervalLowerPage.ReopenCount = 1;

        SyncRequestEntity failedSameIntervalHigherPage = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                      SyncMode.Recovery,
                                                                      SyncRequestStatus.Failed,
                                                                      "2026-04-14T01:30:00Z/2026-04-14T02:00:00Z",
                                                                      4,
                                                                      null);
        failedSameIntervalHigherPage.ReopenCount = 1;

        dbContext.Set<SyncRequestEntity>()
                 .AddRange(pendingNewestInterval,
                           pendingOldestInterval,
                           failedOldestInterval,
                           canceledSameIntervalLowerPage,
                           failedSameIntervalHigherPage);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        List<SyncRequestDto> rows =
                (await sut.GetEligibleRecoveryRequestsAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                            CancellationToken.None)).ToList();

        Assert.Equal(5, rows.Count);
        Assert.Equal(pendingOldestInterval.Id, rows[0].Id);
        Assert.Equal(pendingNewestInterval.Id, rows[1].Id);
        Assert.Equal(failedOldestInterval.Id, rows[2].Id);
        Assert.Equal(canceledSameIntervalLowerPage.Id, rows[3].Id);
        Assert.Equal(failedSameIntervalHigherPage.Id, rows[4].Id);
    }

    #endregion

    #region ========== *** Private Section *** ==========

    private static SyncRequestEntity BuildRequest(string category,
                                                  SyncMode mode,
                                                  SyncRequestStatus status,
                                                  string? interval,
                                                  int? pageNumber,
                                                  string? genesysJobId)
    {
        SyncRequestEntity entity = new SyncRequestEntity
                                   {
                                       PublicId = Guid.NewGuid(),
                                       Category = category,
                                       Mode = mode,
                                       Status = status,
                                       Interval = interval,
                                       PageNumber = pageNumber,
                                       GenesysJobId = genesysJobId
                                   };

        entity.RebuildScopeKey();

        return entity;
    }

    #endregion

    #region ========== *** TryStartNextRecoveryRequestAsync *** ==========

    [Fact]
    public async Task TryStartNextRecoveryRequestAsync_WhenPendingRecoveryExists_StartsAndReturnsRequest()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);

        SyncRequestEntity pending = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                 SyncMode.Recovery,
                                                 SyncRequestStatus.Pending,
                                                 "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z",
                                                 1,
                                                 null);

        dbContext.Set<SyncRequestEntity>()
                 .Add(pending);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns<CancellationToken>(dbContext.SaveChangesAsync);

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        SyncRequestDto? result =
                await sut.TryStartNextRecoveryRequestAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                           CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(pending.Id, result.Id);
        Assert.Equal(SyncRequestStatus.Running, result.Status);
        Assert.Equal(SyncRequestStatus.Running, pending.Status);
        Assert.Equal(0, pending.ReopenCount);
        Assert.Null(pending.CurrentRunId);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ========== *** GetNextJoinableIncrementalRequestAsync *** ==========

    [Fact]
    public async Task GetNextJoinableIncrementalRequestAsync_ReturnsRunningRequestBeforePendingRequest()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);

        SyncRequestEntity pending = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                 SyncMode.Incremental,
                                                 SyncRequestStatus.Pending,
                                                 "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z",
                                                 1,
                                                 null);

        SyncRequestEntity running = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                 SyncMode.Incremental,
                                                 SyncRequestStatus.Running,
                                                 "2026-04-14T01:00:00Z/2026-04-14T01:30:00Z",
                                                 2,
                                                 null);
        running.CurrentRunId = 123;

        dbContext.Set<SyncRequestEntity>()
                 .AddRange(pending, running);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        SyncRequestDto? result =
                await sut.GetNextJoinableIncrementalRequestAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                 CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(running.Id, result.Id);
        Assert.Equal(running.PublicId, result.PublicId);
        Assert.Equal(SyncRequestStatus.Running, result.Status);
        Assert.Equal(running.CurrentRunId, result.CurrentRunId);
    }

    #endregion
}
