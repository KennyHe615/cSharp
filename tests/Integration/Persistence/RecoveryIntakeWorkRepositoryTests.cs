using Application.DTOs.Recovery;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.Recovery;
using Infrastructure.Persistence.Repositories.Recovery;

using Microsoft.EntityFrameworkCore;

using tests.TestSupport.Persistence;
using tests.TestSupport.Time;

using Xunit;


namespace tests.Integration.Persistence;

public sealed class RecoveryIntakeWorkRepositoryTests
{
    [Fact]
    public async Task TryStartNextPendingAsync_WhenPendingRequestExists_MarksOldestPendingAsRunning()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);

        AnalyticsRecoveryRequestEntity first = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                            AnalyticsRecoveryRequestStatus.Pending,
                                                            "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z");

        AnalyticsRecoveryRequestEntity second = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                             AnalyticsRecoveryRequestStatus.Pending,
                                                             "2026-04-14T01:00:00Z/2026-04-14T01:30:00Z");

        dbContext.Set<AnalyticsRecoveryRequestEntity>()
                 .AddRange(first, second);

        await dbContext.SaveChangesAsync();

        RecoveryIntakeWorkRepository sut = new RecoveryIntakeWorkRepository(dbContext);

        AnalyticsRecoveryRequestDto? result =
                await sut.TryStartNextPendingAsync(nameof(SyncAnalyticsCategory.UsersDetails), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(first.Id, result.Id);
        Assert.Equal(AnalyticsRecoveryRequestStatus.Running, result.Status);

        AnalyticsRecoveryRequestEntity started = await dbContext.Set<AnalyticsRecoveryRequestEntity>()
                                                                .SingleAsync(x => x.Id == first.Id);

        AnalyticsRecoveryRequestEntity untouched = await dbContext.Set<AnalyticsRecoveryRequestEntity>()
                                                                  .SingleAsync(x => x.Id == second.Id);

        Assert.Equal(AnalyticsRecoveryRequestStatus.Running, started.Status);
        Assert.Equal(AnalyticsRecoveryRequestStatus.Pending, untouched.Status);
    }

    [Fact]
    public async Task TryStartNextPendingAsync_WhenCategoryFilterProvided_SkipsOtherCategories()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);

        AnalyticsRecoveryRequestEntity conversations = BuildRequest(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                                    AnalyticsRecoveryRequestStatus.Pending,
                                                                    "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z");

        AnalyticsRecoveryRequestEntity users = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                            AnalyticsRecoveryRequestStatus.Pending,
                                                            "2026-04-14T01:00:00Z/2026-04-14T01:30:00Z");

        dbContext.Set<AnalyticsRecoveryRequestEntity>()
                 .AddRange(conversations, users);

        await dbContext.SaveChangesAsync();

        RecoveryIntakeWorkRepository sut = new RecoveryIntakeWorkRepository(dbContext);

        AnalyticsRecoveryRequestDto? result =
                await sut.TryStartNextPendingAsync(nameof(SyncAnalyticsCategory.UsersDetails), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(users.Id, result.Id);
        Assert.Equal(nameof(SyncAnalyticsCategory.UsersDetails), result.Category);

        AnalyticsRecoveryRequestEntity skipped = await dbContext.Set<AnalyticsRecoveryRequestEntity>()
                                                                .SingleAsync(x => x.Id == conversations.Id);

        Assert.Equal(AnalyticsRecoveryRequestStatus.Pending, skipped.Status);
    }

    [Fact]
    public async Task TryStartNextPendingAsync_WhenNoPendingRequest_ReturnsNull()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);

        AnalyticsRecoveryRequestEntity completed = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                AnalyticsRecoveryRequestStatus.Completed,
                                                                "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z");

        dbContext.Set<AnalyticsRecoveryRequestEntity>()
                 .Add(completed);

        await dbContext.SaveChangesAsync();

        RecoveryIntakeWorkRepository sut = new RecoveryIntakeWorkRepository(dbContext);

        AnalyticsRecoveryRequestDto? result =
                await sut.TryStartNextPendingAsync(nameof(SyncAnalyticsCategory.UsersDetails), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryMarkCompletedAsync_WhenRequestIsRunning_MarksCompletedAndClearsFailureReason()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);

        AnalyticsRecoveryRequestEntity running = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                              AnalyticsRecoveryRequestStatus.Running,
                                                              "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z");

        running.FailureReason = "Previous failure";

        dbContext.Set<AnalyticsRecoveryRequestEntity>()
                 .Add(running);

        await dbContext.SaveChangesAsync();

        RecoveryIntakeWorkRepository sut = new RecoveryIntakeWorkRepository(dbContext);

        bool result = await sut.TryMarkCompletedAsync(running.Id, CancellationToken.None);

        AnalyticsRecoveryRequestEntity updated = await dbContext.Set<AnalyticsRecoveryRequestEntity>()
                                                                .SingleAsync(x => x.Id == running.Id);

        Assert.True(result);
        Assert.Equal(AnalyticsRecoveryRequestStatus.Completed, updated.Status);
        Assert.Null(updated.FailureReason);
    }

    [Fact]
    public async Task TryMarkFailedAsync_WhenRequestIsRunning_MarksFailedWithReason()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);

        AnalyticsRecoveryRequestEntity running = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                              AnalyticsRecoveryRequestStatus.Running,
                                                              "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z");

        dbContext.Set<AnalyticsRecoveryRequestEntity>()
                 .Add(running);

        await dbContext.SaveChangesAsync();

        RecoveryIntakeWorkRepository sut = new RecoveryIntakeWorkRepository(dbContext);

        bool result = await sut.TryMarkFailedAsync(running.Id, "Planning failed.", CancellationToken.None);

        AnalyticsRecoveryRequestEntity updated = await dbContext.Set<AnalyticsRecoveryRequestEntity>()
                                                                .SingleAsync(x => x.Id == running.Id);

        Assert.True(result);
        Assert.Equal(AnalyticsRecoveryRequestStatus.Failed, updated.Status);
        Assert.Equal("Planning failed.", updated.FailureReason);
    }

    [Fact]
    public async Task TryMarkCompletedAsync_WhenRequestIsNotRunning_ReturnsFalse()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);

        AnalyticsRecoveryRequestEntity pending = BuildRequest(nameof(SyncAnalyticsCategory.UsersDetails),
                                                              AnalyticsRecoveryRequestStatus.Pending,
                                                              "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z");

        dbContext.Set<AnalyticsRecoveryRequestEntity>()
                 .Add(pending);

        await dbContext.SaveChangesAsync();

        RecoveryIntakeWorkRepository sut = new RecoveryIntakeWorkRepository(dbContext);

        bool result = await sut.TryMarkCompletedAsync(pending.Id, CancellationToken.None);

        Assert.False(result);
    }

    #region ========== *** Private Section *** ==========

    private static AnalyticsRecoveryRequestEntity BuildRequest(string category,
                                                               AnalyticsRecoveryRequestStatus status,
                                                               string interval)
    {
        AnalyticsRecoveryRequestEntity entity = new AnalyticsRecoveryRequestEntity
                                                {
                                                    Category = category,
                                                    Status = status,
                                                    Interval = interval
                                                };

        entity.RebuildScopeKey();

        return entity;
    }

    #endregion
}
