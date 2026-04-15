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

    [Fact]
    public async Task CreateOrGetByScopeAsync_WhenScopeAlreadyExists_ReturnsExistingId_AndSkipsUnitOfWork()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);

        SyncRequestEntity existing = new SyncRequestEntity
                                     {
                                         Category = nameof(SyncReferenceCategory.Group),
                                         Mode = SyncMode.Incremental
                                     };
        existing.RebuildScopeKey();

        dbContext.Set<SyncRequestEntity>()
                 .Add(existing);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        long result = await sut.CreateOrGetByScopeAsync(nameof(SyncReferenceCategory.Group),
                                                        SyncMode.Incremental,
                                                        null,
                                                        null,
                                                        null,
                                                        CancellationToken.None);

        Assert.Equal(existing.Id, result);
        uow.Verify(x => x.UpsertAsync(It.IsAny<SyncRequestEntity>(),
                                      It.IsAny<Action<SyncRequestEntity>>(),
                                      It.IsAny<CancellationToken>()),
                   Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrGetByScopeAsync_WhenScopeDoesNotExist_CreatesAndReturnsNewId()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

        uow.Setup(x => x.UpsertAsync(It.IsAny<SyncRequestEntity>(), null, It.IsAny<CancellationToken>()))
           .Callback<object, Action<SyncRequestEntity>?, CancellationToken>((entity, _, _) =>
                                                                            {
                                                                                SyncRequestEntity request =
                                                                                    (SyncRequestEntity)entity;
                                                                                request.Id = 777;
                                                                            })
           .Returns(Task.CompletedTask);

        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .ReturnsAsync(1);

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        long result = await sut.CreateOrGetByScopeAsync(nameof(SyncReferenceCategory.Skill),
                                                        SyncMode.Incremental,
                                                        "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z",
                                                        1,
                                                        "JOB-1",
                                                        CancellationToken.None);

        Assert.Equal(777, result);

        uow.Verify(x => x.UpsertAsync(It.Is<SyncRequestEntity>(e => e.Category == nameof(SyncReferenceCategory.Skill)
                                                                    && e.Mode  == SyncMode.Incremental
                                                                    && e.Interval
                                                                    == "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z"
                                                                    && e.PageNumber   == 1
                                                                    && e.GenesysJobId == "JOB-1"
                                                                    && !string.IsNullOrWhiteSpace(e.ScopeKey)),
                                      null,
                                      It.IsAny<CancellationToken>()),
                   Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrGetByScopeAsync_WhenSaveHitsUniqueScopeRace_ReturnsWinnerId()
    {
        string dbName = Guid.NewGuid()
                            .ToString("N");

        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object, dbName);
        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

        const string category = nameof(SyncReferenceCategory.WrapUpCode);
        const string interval = "2026-04-14T01:00:00Z/2026-04-14T01:30:00Z";

        uow.Setup(x => x.UpsertAsync(It.IsAny<SyncRequestEntity>(), null, It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Callback<CancellationToken>(_ =>
                                        {
                                            Mock<IDateTimeProvider> raceDateTimeProvider =
                                                DateTimeProviderTestFactory.Create();

                                            using AppDbContext raceContext =
                                                PersistenceTestFactory.CreateDbContext(raceDateTimeProvider.Object,
                                                 dbName);

                                            SyncRequestEntity winner = new SyncRequestEntity
                                                                       {
                                                                           Category = category,
                                                                           Mode = SyncMode.Incremental,
                                                                           Interval = interval
                                                                       };
                                            winner.RebuildScopeKey();

                                            raceContext.Set<SyncRequestEntity>()
                                                       .Add(winner);
                                            raceContext.SaveChanges();

                                            throw new DbUpdateException("duplicate",
                                                                        new Exception("UX_sync_request_scope_key"));
                                        })
           .ReturnsAsync(0);

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        long result = await sut.CreateOrGetByScopeAsync(category,
                                                        SyncMode.Incremental,
                                                        interval,
                                                        null,
                                                        null,
                                                        CancellationToken.None);

        SyncRequestEntity winnerRow = await dbContext.Set<SyncRequestEntity>()
                                                     .SingleAsync(x => x.Category    == category
                                                                       && x.Interval == interval);

        Assert.Equal(winnerRow.Id, result);
    }

    #endregion

    #region ========== *** GetByIdAsync *** ==========

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsProjectedDto()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);

        SyncRequestEntity entity = new SyncRequestEntity
                                   {
                                       Category = nameof(SyncAnalyticsCategory.UsersDetails),
                                       Mode = SyncMode.Recovery,
                                       Interval = null,
                                       PageNumber = 3,
                                       GenesysJobId = "JOB-123",
                                       CurrentRunId = 99
                                   };
        entity.RebuildScopeKey();

        dbContext.Set<SyncRequestEntity>()
                 .Add(entity);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        SyncRequestDto? dto = await sut.GetByIdAsync(entity.Id, CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.Category, dto.Category);
        Assert.Equal(entity.Mode, dto.Mode);
        Assert.Equal(entity.Interval, dto.Interval);
        Assert.Equal(entity.PageNumber, dto.PageNumber);
        Assert.Equal(entity.GenesysJobId, dto.GenesysJobId);
        Assert.Equal(entity.ScopeKey, dto.ScopeKey);
        Assert.Equal(entity.CurrentRunId, dto.CurrentRunId);
    }

    #endregion

    #region ========== *** SetCurrentRunAsync *** ==========

    [Fact]
    public async Task SetCurrentRunAsync_WhenRunBelongsToRequest_SetsPointer_AndSaves()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);

        SyncRequestEntity request = new SyncRequestEntity
                                    {
                                        Category = nameof(SyncReferenceCategory.Group),
                                        Mode = SyncMode.Incremental
                                    };
        request.RebuildScopeKey();

        dbContext.Set<SyncRequestEntity>()
                 .Add(request);
        await dbContext.SaveChangesAsync();

        SyncRunEntity run = new SyncRunEntity
                            {
                                RequestId = request.Id,
                                Status = SyncRunStatus.Running
                            };

        dbContext.Set<SyncRunEntity>()
                 .Add(run);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .ReturnsAsync(1);

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        await sut.SetCurrentRunAsync(request.Id, run.Id, CancellationToken.None);

        Assert.Equal(run.Id, request.CurrentRunId);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetCurrentRunAsync_WhenRunBelongsToDifferentRequest_ThrowsInvalidOperationException()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateDbContext(dateTimeProvider.Object);

        SyncRequestEntity request1 = new SyncRequestEntity
                                     {
                                         Category = nameof(SyncReferenceCategory.Group),
                                         Mode = SyncMode.Incremental
                                     };
        request1.RebuildScopeKey();

        SyncRequestEntity request2 = new SyncRequestEntity
                                     {
                                         Category = nameof(SyncReferenceCategory.Skill),
                                         Mode = SyncMode.Incremental
                                     };
        request2.RebuildScopeKey();

        dbContext.Set<SyncRequestEntity>()
                 .AddRange(request1, request2);
        await dbContext.SaveChangesAsync();

        SyncRunEntity run = new SyncRunEntity
                            {
                                RequestId = request2.Id,
                                Status = SyncRunStatus.Running
                            };

        dbContext.Set<SyncRunEntity>()
                 .Add(run);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

        SyncRequestRepository sut = new SyncRequestRepository(dbContext, uow.Object);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SetCurrentRunAsync(request1.Id,
                                                                 run.Id,
                                                                 CancellationToken.None));

        Assert.Contains("does not belong", ex.Message, StringComparison.Ordinal);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
