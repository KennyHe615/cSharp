using Application.Abstractions.Persistence;
using Application.Abstractions.Persistence.SyncTracking;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;
using Infrastructure.Persistence.Repositories.SyncTracking;

using Microsoft.EntityFrameworkCore;

using Moq;

using SharedKernel.Lobs;
using SharedKernel.Time;

using tests.TestSupport.Persistence;
using tests.TestSupport.Time;

using Xunit;


namespace tests.Integration.Persistence;

/// <summary>
/// Integration tests for <see cref="IncrementalSyncWindowRepository"/>.
/// </summary>
public sealed class IncrementalSyncWindowRepositoryTests
{
    /// <summary>
    /// Verifies that the first reservation creates the window row and starts from the current Eastern start-of-day.
    /// </summary>
    [Fact]
    public async Task ReserveNextWindowAsync_WhenWindowDoesNotExist_CreatesRowFromEasternStartOfDay()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateMockUnitOfWork<IncrementalSyncWindowEntity>(dbContext);

        IncrementalSyncWindowRepository sut = new IncrementalSyncWindowRepository(dbContext, uow.Object);

        DateTimeOffset intervalEndEastern = new DateTimeOffset(2026,
                                                               5,
                                                               4,
                                                               10,
                                                               17,
                                                               12,
                                                               TimeSpan.FromHours(-4));

        IncrementalSyncWindowReservation result =
                await sut.ReserveNextWindowAsync(LobName.Ntt,
                                                 SyncAnalyticsCategory.UsersDetails,
                                                 intervalEndEastern,
                                                 CancellationToken.None);

        IncrementalSyncWindowEntity row = await dbContext.Set<IncrementalSyncWindowEntity>()
                                                         .SingleAsync();

        Assert.True(result.Reserved);
        Assert.Equal("2026-05-04T04:00Z/2026-05-04T14:17Z", result.IntervalUtc);
        Assert.Equal(new DateTimeOffset(2026,
                                        5,
                                        4,
                                        4,
                                        0,
                                        0,
                                        TimeSpan.Zero),
                     result.StartUtc);
        Assert.Equal(new DateTimeOffset(2026,
                                        5,
                                        4,
                                        14,
                                        17,
                                        0,
                                        TimeSpan.Zero),
                     result.EndUtc);

        Assert.Equal(SyncAnalyticsCategory.UsersDetails, row.Category);
        Assert.Equal(new DateTimeOffset(2026,
                                        5,
                                        4,
                                        14,
                                        17,
                                        0,
                                        TimeSpan.Zero),
                     row.NextIntervalStartUtc);
        Assert.Equal(new DateTimeOffset(2026,
                                        5,
                                        4,
                                        4,
                                        0,
                                        0,
                                        TimeSpan.Zero),
                     row.LastReservedStartUtc);
        Assert.Equal(new DateTimeOffset(2026,
                                        5,
                                        4,
                                        14,
                                        17,
                                        0,
                                        TimeSpan.Zero),
                     row.LastReservedEndUtc);
    }

    /// <summary>
    /// Verifies that a later reservation advances from the persisted cursor instead of resetting to start-of-day.
    /// </summary>
    [Fact]
    public async Task ReserveNextWindowAsync_WhenWindowExists_AdvancesFromPersistedNextStart()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateMockUnitOfWork<IncrementalSyncWindowEntity>(dbContext);

        dbContext.Set<IncrementalSyncWindowEntity>()
                 .Add(new IncrementalSyncWindowEntity
                      {
                          Category = SyncAnalyticsCategory.UsersDetails,
                          NextIntervalStartUtc = new DateTimeOffset(2026,
                                                                    5,
                                                                    4,
                                                                    14,
                                                                    17,
                                                                    0,
                                                                    TimeSpan.Zero)
                      });
        await dbContext.SaveChangesAsync();

        IncrementalSyncWindowRepository sut = new IncrementalSyncWindowRepository(dbContext, uow.Object);

        DateTimeOffset intervalEndEastern = new DateTimeOffset(2026,
                                                               5,
                                                               4,
                                                               10,
                                                               47,
                                                               45,
                                                               TimeSpan.FromHours(-4));

        IncrementalSyncWindowReservation result =
                await sut.ReserveNextWindowAsync(LobName.Ntt,
                                                 SyncAnalyticsCategory.UsersDetails,
                                                 intervalEndEastern,
                                                 CancellationToken.None);

        IncrementalSyncWindowEntity row = await dbContext.Set<IncrementalSyncWindowEntity>()
                                                         .SingleAsync();

        Assert.True(result.Reserved);
        Assert.Equal("2026-05-04T14:17Z/2026-05-04T14:47Z", result.IntervalUtc);
        Assert.Equal(new DateTimeOffset(2026,
                                        5,
                                        4,
                                        14,
                                        17,
                                        0,
                                        TimeSpan.Zero),
                     result.StartUtc);
        Assert.Equal(new DateTimeOffset(2026,
                                        5,
                                        4,
                                        14,
                                        47,
                                        0,
                                        TimeSpan.Zero),
                     result.EndUtc);

        Assert.Equal(new DateTimeOffset(2026,
                                        5,
                                        4,
                                        14,
                                        47,
                                        0,
                                        TimeSpan.Zero),
                     row.NextIntervalStartUtc);
        Assert.Equal(new DateTimeOffset(2026,
                                        5,
                                        4,
                                        14,
                                        17,
                                        0,
                                        TimeSpan.Zero),
                     row.LastReservedStartUtc);
        Assert.Equal(new DateTimeOffset(2026,
                                        5,
                                        4,
                                        14,
                                        47,
                                        0,
                                        TimeSpan.Zero),
                     row.LastReservedEndUtc);
    }

    /// <summary>
    /// Verifies that no reservation is returned when the requested end does not move past the persisted cursor.
    /// </summary>
    [Fact]
    public async Task ReserveNextWindowAsync_WhenEndDoesNotAdvance_ReturnsNotReserved()
    {
        Mock<IDateTimeProvider> dateTimeProvider = DateTimeProviderTestFactory.Create();

        await using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider.Object);
        Mock<IUnitOfWork> uow = PersistenceTestFactory.CreateMockUnitOfWork<IncrementalSyncWindowEntity>(dbContext);

        dbContext.Set<IncrementalSyncWindowEntity>()
                 .Add(new IncrementalSyncWindowEntity
                      {
                          Category = SyncAnalyticsCategory.UsersDetails,
                          NextIntervalStartUtc = new DateTimeOffset(2026,
                                                                    5,
                                                                    4,
                                                                    14,
                                                                    47,
                                                                    0,
                                                                    TimeSpan.Zero)
                      });
        await dbContext.SaveChangesAsync();

        IncrementalSyncWindowRepository sut = new IncrementalSyncWindowRepository(dbContext, uow.Object);

        DateTimeOffset intervalEndEastern = new DateTimeOffset(2026,
                                                               5,
                                                               4,
                                                               10,
                                                               47,
                                                               10,
                                                               TimeSpan.FromHours(-4));

        IncrementalSyncWindowReservation result =
                await sut.ReserveNextWindowAsync(LobName.Ntt,
                                                 SyncAnalyticsCategory.UsersDetails,
                                                 intervalEndEastern,
                                                 CancellationToken.None);

        IncrementalSyncWindowEntity row = await dbContext.Set<IncrementalSyncWindowEntity>()
                                                         .SingleAsync();

        Assert.False(result.Reserved);
        Assert.Null(result.IntervalUtc);
        Assert.Null(result.StartUtc);
        Assert.Null(result.EndUtc);

        Assert.Equal(new DateTimeOffset(2026,
                                        5,
                                        4,
                                        14,
                                        47,
                                        0,
                                        TimeSpan.Zero),
                     row.NextIntervalStartUtc);
        Assert.Null(row.LastReservedStartUtc);
        Assert.Null(row.LastReservedEndUtc);
    }
}
