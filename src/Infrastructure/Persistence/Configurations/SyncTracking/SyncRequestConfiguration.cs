using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.SyncTracking;

public sealed class SyncRequestConfiguration : IEntityTypeConfiguration<SyncRequestEntity>
{
    public void Configure(EntityTypeBuilder<SyncRequestEntity> builder)
    {
        builder.ToTable("sync_request", "dbo");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Id)
               .ValueGeneratedOnAdd();

        builder.Property(x => x.Category)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(x => x.Mode)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(x => x.Interval)
               .HasMaxLength(50);

        builder.Property(x => x.PageNumber);

        builder.Property(x => x.GenesysJobId)
               .HasMaxLength(100);

        builder.Property(x => x.ScopeKey)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(x => x.CurrentRunId);

        #endregion

        #region ========== *** Relationships *** ==========

        builder.HasMany(x => x.Runs)
               .WithOne(x => x.Request)
               .HasForeignKey(x => x.RequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CurrentRun)
               .WithMany()
               .HasForeignKey(x => x.CurrentRunId)
               .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.ScopeKey)
               .IsUnique()
               .HasDatabaseName("UX_sync_request_scope_key");

        builder.HasIndex(x => new { x.Category, x.Mode, x.AppUpdatedAt })
               .HasDatabaseName("IX_sync_request_category_mode_app_updated_at");

        builder.HasIndex(x => x.CurrentRunId)
               .HasDatabaseName("IX_sync_request_current_run_id");

        #endregion
    }
}
