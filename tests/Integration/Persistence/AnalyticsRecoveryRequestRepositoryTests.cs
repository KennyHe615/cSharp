using Application.Abstractions.Persistence;
using Application.DTOs.Recovery;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.Recovery;
using Infrastructure.Persistence.Repositories.Recovery;

using Microsoft.EntityFrameworkCore;

using Moq;

using SharedKernel.Time;

using tests.TestSupport.Persistence;
using tests.TestSupport.Time;

using Xunit;


namespace tests.Integration.Persistence;

public sealed class AnalyticsRecoveryRequestRepositoryTests
{
    #region ========== *** CreateOrGetActiveAsync *** ==========

    [Fact]
    public async Task CreateOrGetActiveAsync_WhenActiveScopeExists_ReturnsExistingActiveResult()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);

        AnalyticsRecoveryRequestEntity existing = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                               AnalyticsRecoveryRequestStatus.Running,
                                                               "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z",
                                                               null);

        dbContext.Set<AnalyticsRecoveryRequestEntity>()
                 .Add(existing);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

        AnalyticsRecoveryRequestRepository sut = new AnalyticsRecoveryRequestRepository(dbContext, uow.Object);

        AnalyticsRecoveryRequestResolveResult result =
                await sut.CreateOrGetActiveAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                 "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z",
                                                 null,
                                                 CancellationToken.None);

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(existing.PublicId, result.PublicId);
        Assert.Equal(AnalyticsRecoveryRequestResolveAction.ReusedActive, result.RequestAction);

        uow.Verify(x => x.UpsertAsync(It.IsAny<AnalyticsRecoveryRequestEntity>(),
                                      It.IsAny<Action<AnalyticsRecoveryRequestEntity>?>(),
                                      It.IsAny<CancellationToken>()),
                   Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrGetActiveAsync_WhenScopeDoesNotExist_CreatesPendingRequest()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<AnalyticsRecoveryRequestEntity>(dbContext);

        AnalyticsRecoveryRequestRepository sut = new AnalyticsRecoveryRequestRepository(dbContext, uow.Object);

        AnalyticsRecoveryRequestResolveResult result =
                await sut.CreateOrGetActiveAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                 "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z",
                                                 null,
                                                 CancellationToken.None);

        AnalyticsRecoveryRequestEntity created = await dbContext.Set<AnalyticsRecoveryRequestEntity>()
                                                                .SingleAsync();

        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.PublicId, result.PublicId);
        Assert.Equal(AnalyticsRecoveryRequestResolveAction.Created, result.RequestAction);
        Assert.Equal(nameof(SyncAnalyticsCategory.UsersDetails), created.Category);
        Assert.Equal(AnalyticsRecoveryRequestStatus.Pending, created.Status);
        Assert.Equal("2026-04-14T00:00:00Z/2026-04-14T00:30:00Z", created.Interval);
        Assert.Null(created.GenesysJobId);
        Assert.False(string.IsNullOrWhiteSpace(created.ScopeKey));

        uow.Verify(x => x.UpsertAsync(It.IsAny<AnalyticsRecoveryRequestEntity>(), null, It.IsAny<CancellationToken>()),
                   Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(AnalyticsRecoveryRequestStatus.Completed)]
    [InlineData(AnalyticsRecoveryRequestStatus.Failed)]
    [InlineData(AnalyticsRecoveryRequestStatus.Canceled)]
    public async Task CreateOrGetActiveAsync_WhenLatestScopeIsTerminal_CreatesNewRequest(
            AnalyticsRecoveryRequestStatus status)
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateUnitOfWork<AnalyticsRecoveryRequestEntity>(dbContext);

        AnalyticsRecoveryRequestEntity terminal = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                               status,
                                                               "2026-04-14T01:00:00Z/2026-04-14T01:30:00Z",
                                                               null);

        dbContext.Set<AnalyticsRecoveryRequestEntity>()
                 .Add(terminal);
        await dbContext.SaveChangesAsync();

        AnalyticsRecoveryRequestRepository sut = new AnalyticsRecoveryRequestRepository(dbContext, uow.Object);

        AnalyticsRecoveryRequestResolveResult result =
                await sut.CreateOrGetActiveAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                 "2026-04-14T01:00:00Z/2026-04-14T01:30:00Z",
                                                 null,
                                                 CancellationToken.None);

        List<AnalyticsRecoveryRequestEntity> rows = await dbContext.Set<AnalyticsRecoveryRequestEntity>()
                                                                   .OrderBy(x => x.Id)
                                                                   .ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal(terminal.Id, rows[0].Id);
        Assert.Equal(rows[1].Id, result.Id);
        Assert.Equal(rows[1].PublicId, result.PublicId);
        Assert.Equal(AnalyticsRecoveryRequestResolveAction.Created, result.RequestAction);
        Assert.Equal(status, rows[0].Status);
        Assert.Equal(AnalyticsRecoveryRequestStatus.Pending, rows[1].Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRequestExists_ReturnsDto()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);

        AnalyticsRecoveryRequestEntity existing = BuildRequest(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                               AnalyticsRecoveryRequestStatus.Pending,
                                                               null,
                                                               "JOB-123");

        dbContext.Set<AnalyticsRecoveryRequestEntity>()
                 .Add(existing);
        await dbContext.SaveChangesAsync();

        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        AnalyticsRecoveryRequestRepository sut = new AnalyticsRecoveryRequestRepository(dbContext, uow.Object);

        AnalyticsRecoveryRequestDto? result = await sut.GetByIdAsync(existing.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(existing.PublicId, result.PublicId);
        Assert.Equal(existing.Category, result.Category);
        Assert.Equal(existing.Status, result.Status);
        Assert.Equal(existing.Interval, result.Interval);
        Assert.Equal(existing.GenesysJobId, result.GenesysJobId);
        Assert.Equal(existing.FailureReason, result.FailureReason);
    }

    #endregion

    #region ========== *** Private Section *** ==========

    private static AnalyticsRecoveryRequestEntity BuildRequest(string category,
                                                               AnalyticsRecoveryRequestStatus status,
                                                               string? interval,
                                                               string? genesysJobId)
    {
        AnalyticsRecoveryRequestEntity entity = new AnalyticsRecoveryRequestEntity
                                                {
                                                    Category = category,
                                                    Status = status,
                                                    Interval = interval,
                                                    GenesysJobId = genesysJobId
                                                };

        entity.RebuildScopeKey();

        return entity;
    }

    #endregion
}
