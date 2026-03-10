using Application.DTOs.SyncTracking;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;
using Infrastructure.Persistence.Repositories.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using tests.TestSupport.Persistence;

using Xunit;


namespace tests.Integration.Persistence;

public sealed class SyncRequestRepositoryTests
{
    [Fact]
    public async Task CreateOrGetByScopeAsync_NewScope_CreatesRecord()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRequestRepository sut =
            SyncTrackingPersistenceTestFixture.CreateRequestRepository(scope.ServiceProvider, db);

        long id = await sut.CreateOrGetByScopeAsync(SyncCategory.UsersDetails,
                                                    SyncMode.Incremental,
                                                    "2026-03-09T00:00Z/2026-03-09T00:30Z",
                                                    null,
                                                    null,
                                                    CancellationToken.None);

        SyncRequestEntity row = await db.Set<SyncRequestEntity>()
                                        .SingleAsync(x => x.Id == id);

        Assert.Equal(SyncCategory.UsersDetails, row.Category);
        Assert.Equal(SyncMode.Incremental, row.Mode);
        Assert.Equal("2026-03-09T00:00Z/2026-03-09T00:30Z", row.Interval);
        Assert.Null(row.PageNumber);
        Assert.Null(row.GenesysJobId);
        Assert.False(string.IsNullOrWhiteSpace(row.ScopeKey));
    }

    [Fact]
    public async Task CreateOrGetByScopeAsync_SameScope_ReturnsExistingId()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRequestRepository sut =
            SyncTrackingPersistenceTestFixture.CreateRequestRepository(scope.ServiceProvider, db);

        long firstId = await sut.CreateOrGetByScopeAsync(SyncCategory.UsersDetails,
                                                         SyncMode.Incremental,
                                                         "2026-03-09T00:00Z/2026-03-09T00:30Z",
                                                         null,
                                                         null,
                                                         CancellationToken.None);

        long secondId = await sut.CreateOrGetByScopeAsync(SyncCategory.UsersDetails,
                                                          SyncMode.Incremental,
                                                          "2026-03-09T00:00Z/2026-03-09T00:30Z",
                                                          null,
                                                          null,
                                                          CancellationToken.None);

        int count = await db.Set<SyncRequestEntity>()
                            .CountAsync();

        Assert.Equal(firstId, secondId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProjectedDto()
    {
        await using ServiceProvider provider = SyncTrackingPersistenceTestFixture.BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        SyncRequestEntity entity = new SyncRequestEntity
                                   {
                                       Category = SyncCategory.ConversationsDetails,
                                       Mode = SyncMode.Recovery,
                                       Interval = null,
                                       PageNumber = 3,
                                       GenesysJobId = "JOB-123",
                                       CurrentRunId = 99
                                   };
        entity.RebuildScopeKey();

        db.Set<SyncRequestEntity>()
          .Add(entity);
        await db.SaveChangesAsync();

        SyncRequestRepository sut =
            SyncTrackingPersistenceTestFixture.CreateRequestRepository(scope.ServiceProvider, db);

        SyncRequestDto? dto = await sut.GetByIdAsync(entity.Id, CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(SyncCategory.ConversationsDetails, dto.Category);
        Assert.Equal(SyncMode.Recovery, dto.Mode);
        Assert.Null(dto.Interval);
        Assert.Equal(3, dto.PageNumber);
        Assert.Equal("JOB-123", dto.GenesysJobId);
        Assert.Equal(entity.ScopeKey, dto.ScopeKey);
        Assert.Equal(99, dto.CurrentRunId);
    }
}
