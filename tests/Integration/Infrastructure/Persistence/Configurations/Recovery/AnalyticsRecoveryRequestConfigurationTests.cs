using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.Recovery;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using tests.TestSupport.Persistence;
using tests.TestSupport.Time;

using Xunit;


namespace tests.Integration.Infrastructure.Persistence.Configurations.Recovery;

public sealed class AnalyticsRecoveryRequestConfigurationTests
{
    [Fact]
    public void AnalyticsRecoveryRequest_Model_HasExpectedTableAndColumns()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);

        IEntityType entityType = EntityModelTestHelper.GetEntityType<AnalyticsRecoveryRequestEntity>(dbContext);

        Assert.Equal("analytics_recovery_request", entityType.GetTableName());

        Assert.False(entityType.FindProperty(nameof(AnalyticsRecoveryRequestEntity.PublicId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(AnalyticsRecoveryRequestEntity.Category))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(AnalyticsRecoveryRequestEntity.Status))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(AnalyticsRecoveryRequestEntity.ScopeKey))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(AnalyticsRecoveryRequestEntity.Interval))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(AnalyticsRecoveryRequestEntity.GenesysJobId))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(AnalyticsRecoveryRequestEntity.FailureReason))!.IsNullable);
    }

    [Fact]
    public void AnalyticsRecoveryRequest_Model_HasFilteredActiveScopeUniqueIndex()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);

        IEntityType entityType = EntityModelTestHelper.GetEntityType<AnalyticsRecoveryRequestEntity>(dbContext);

        IIndex index = EntityModelTestHelper.FindIndex(entityType, nameof(AnalyticsRecoveryRequestEntity.ScopeKey));

        Assert.True(index.IsUnique);
        Assert.Equal("[status] IN ('PENDING','RUNNING')", index.GetFilter());
    }

    [Fact]
    public void AnalyticsRecoveryRequest_Model_HasPlannerQueryIndexes()
    {
        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();

        using AppDbContext dbContext = PersistenceTestFactory.CreateInMemoryDbContext(dateTimeProvider);

        IEntityType entityType = EntityModelTestHelper.GetEntityType<AnalyticsRecoveryRequestEntity>(dbContext);

        Assert.True(EntityModelTestHelper.HasIndex(entityType,
                                                   nameof(AnalyticsRecoveryRequestEntity.Category),
                                                   nameof(AnalyticsRecoveryRequestEntity.Status),
                                                   nameof(AnalyticsRecoveryRequestEntity.AppUpdatedAtEastern)));

        Assert.True(EntityModelTestHelper.HasIndex(entityType,
                                                   nameof(AnalyticsRecoveryRequestEntity.AppUpdatedAtEastern)));
    }
}
