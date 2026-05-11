using Infrastructure.Persistence.Entities.Recovery;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.Recovery;

/// <summary>
/// Entity Framework configuration for user-submitted analytics recovery intake requests.
/// </summary>
public sealed class AnalyticsRecoveryRequestConfiguration : IEntityTypeConfiguration<AnalyticsRecoveryRequestEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AnalyticsRecoveryRequestEntity> builder)
    {
        builder.ToTable("analytics_recovery_request", "dbo");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Id)
               .ValueGeneratedOnAdd();

        builder.Property(x => x.PublicId)
               .IsRequired()
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.Category)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(x => x.Status)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(x => x.Interval)
               .HasMaxLength(50);

        builder.Property(x => x.GenesysJobId)
               .HasMaxLength(100);

        builder.Property(x => x.FailureReason)
               .HasMaxLength(1000);

        builder.Property(x => x.ScopeKey)
               .IsRequired()
               .HasMaxLength(255);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.PublicId)
               .IsUnique()
               .HasDatabaseName("UX_analytics_recovery_request_public_id");

        builder.HasIndex(x => x.ScopeKey)
               .IsUnique()
               .HasFilter("[status] IN ('PENDING','RUNNING')")
               .HasDatabaseName("UX_analytics_recovery_request_scope_key_active");

        builder.HasIndex(x => new { x.Category, x.Status, x.AppUpdatedAtEastern })
               .HasDatabaseName("IX_analytics_recovery_request_category_status_app_updated_at_eastern");

        builder.HasIndex(x => x.AppUpdatedAtEastern)
               .HasDatabaseName("IX_analytics_recovery_request_app_updated_at_eastern");

        #endregion
    }
}
