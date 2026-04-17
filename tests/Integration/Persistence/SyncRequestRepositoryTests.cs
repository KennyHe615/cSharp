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

    #region ========== ** Incremental ** ==========

    [Fact]
    public async Task
        CreateOrGetByScopeAsync_WhenIncrementalScopeAlreadyExists_ReturnsExistingResult_AndSkipsUnitOfWork()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);

        SyncRequestEntity existing = BuildRequest(nameof(SyncReferenceCategory.Group),
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

        SyncRequestResolveResult result = await sut.CreateOrGetByScopeAsync(nameof(SyncReferenceCategory.Group),
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

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRequestEntity>(dbContext);

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        SyncRequestResolveResult result = await sut.CreateOrGetByScopeAsync(nameof(SyncReferenceCategory.Skill),
                                                                            SyncMode.Incremental,
                                                                            "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z",
                                                                            1,
                                                                            "JOB-1",
                                                                            CancellationToken.None);

        SyncRequestEntity created = await dbContext.Set<SyncRequestEntity>()
                                                   .SingleAsync();

        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.PublicId, result.PublicId);
        Assert.Equal(SyncRequestResolveAction.Created, result.RequestAction);
        Assert.Equal(nameof(SyncReferenceCategory.Skill), created.Category);
        Assert.Equal(SyncMode.Incremental, created.Mode);
        Assert.Equal(SyncRequestStatus.Pending, created.Status);
        Assert.Equal(0, created.ReopenCount);
        Assert.Equal("2026-04-14T00:00:00Z/2026-04-14T00:30:00Z", created.Interval);
        Assert.Equal(1, created.PageNumber);
        Assert.Equal("JOB-1", created.GenesysJobId);
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

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);

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

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);

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

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRequestEntity>(dbContext);

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
    public async Task CreateOrGetByScopeAsync_WhenRecoveryRowsTieOnUpdatedAt_UsesHighestIdAsLatest()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<SyncRequestEntity>(dbContext);

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

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);

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
}
