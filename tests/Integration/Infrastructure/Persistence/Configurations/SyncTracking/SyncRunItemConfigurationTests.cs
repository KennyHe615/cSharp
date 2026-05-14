using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using tests.TestSupport.Persistence;
using tests.TestSupport.Time;

using Xunit;


namespace tests.Integration.Infrastructure.Persistence.Configurations.SyncTracking;

public sealed class SyncRunItemConfigurationTests
{
    [Fact]
    public void SyncRunItem_Model_HasPageAndLeaseColumns()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);

        IEntityType entityType = EntityModelTestHelper.GetEntityType<SyncRunItemEntity>(dbContext);

        Assert.Equal("sync_run_item", entityType.GetTableName());

        Assert.True(entityType.FindProperty(nameof(SyncRunItemEntity.Cursor))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(SyncRunItemEntity.PageNumber))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(SyncRunItemEntity.ClaimedBy))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(SyncRunItemEntity.LeaseToken))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(SyncRunItemEntity.ClaimedAtEastern))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(SyncRunItemEntity.ClaimExpiresAtEastern))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(SyncRunItemEntity.LastHeartbeatAtEastern))!.IsNullable);

        IProperty attemptCount = entityType.FindProperty(nameof(SyncRunItemEntity.AttemptCount))
                                 ?? throw new InvalidOperationException("AttemptCount property was not found.");

        Assert.False(attemptCount.IsNullable);
        Assert.Equal(0, attemptCount.GetDefaultValue());
    }

    [Fact]
    public void SyncRunItem_Model_HasSelectorCheckConstraint()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);

        IEntityType entityType = EntityModelTestHelper.GetEntityType<SyncRunItemEntity>(dbContext);

        ICheckConstraint constraint = entityType.GetCheckConstraints()
                                                .Single(x => x.Name == "CK_sync_run_item_selector_shape");

        Assert.Contains("[page_number] IS NULL AND [cursor] IS NOT NULL", constraint.Sql, StringComparison.Ordinal);
        Assert.Contains("[page_number] IS NOT NULL AND [cursor] IS NULL", constraint.Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncRunItem_Model_HasFilteredCursorAndPageUniqueIndexes()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);

        IEntityType entityType = EntityModelTestHelper.GetEntityType<SyncRunItemEntity>(dbContext);

        IIndex cursorIndex = EntityModelTestHelper.FindIndex(entityType,
                                                             nameof(SyncRunItemEntity.RunId),
                                                             nameof(SyncRunItemEntity.Step),
                                                             nameof(SyncRunItemEntity.Cursor));

        Assert.True(cursorIndex.IsUnique);
        Assert.Equal("[page_number] IS NULL AND [cursor] IS NOT NULL", cursorIndex.GetFilter());

        IIndex pageIndex = EntityModelTestHelper.FindIndex(entityType,
                                                           nameof(SyncRunItemEntity.RunId),
                                                           nameof(SyncRunItemEntity.Step),
                                                           nameof(SyncRunItemEntity.PageNumber));

        Assert.True(pageIndex.IsUnique);
        Assert.Equal("[page_number] IS NOT NULL", pageIndex.GetFilter());
    }

    [Fact]
    public void SyncRunItem_Model_HasClaimAndLeaseIndexes()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);

        IEntityType entityType = EntityModelTestHelper.GetEntityType<SyncRunItemEntity>(dbContext);

        Assert.True(EntityModelTestHelper.HasIndex(entityType,
                                                   nameof(SyncRunItemEntity.RunId),
                                                   nameof(SyncRunItemEntity.Step),
                                                   nameof(SyncRunItemEntity.Status),
                                                   nameof(SyncRunItemEntity.ClaimExpiresAtEastern),
                                                   nameof(SyncRunItemEntity.PageNumber)));

        Assert.True(EntityModelTestHelper.HasIndex(entityType,
                                                   nameof(SyncRunItemEntity.RunId),
                                                   nameof(SyncRunItemEntity.Step),
                                                   nameof(SyncRunItemEntity.Status),
                                                   nameof(SyncRunItemEntity.ClaimExpiresAtEastern),
                                                   nameof(SyncRunItemEntity.Cursor)));

        Assert.True(EntityModelTestHelper.HasIndex(entityType,
                                                   nameof(SyncRunItemEntity.RunId),
                                                   nameof(SyncRunItemEntity.Step),
                                                   nameof(SyncRunItemEntity.ClaimedBy),
                                                   nameof(SyncRunItemEntity.Status)));

        Assert.True(EntityModelTestHelper.HasIndex(entityType, nameof(SyncRunItemEntity.LeaseToken)));
    }
}
