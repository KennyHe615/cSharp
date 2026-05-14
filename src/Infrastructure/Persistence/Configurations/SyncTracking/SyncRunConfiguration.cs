using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.SyncTracking;

public sealed class SyncRunConfiguration : IEntityTypeConfiguration<SyncRunEntity>
{
    public void Configure(EntityTypeBuilder<SyncRunEntity> builder)
    {
        builder.ToTable("sync_run", "dbo");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Id)
               .ValueGeneratedOnAdd();

        builder.Property(x => x.RequestId)
               .IsRequired();

        builder.Property(x => x.Status)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(x => x.SupersededByRunId);

        builder.Property(x => x.AttemptNo)
               .IsRequired();

        builder.Property(x => x.RunStartedAtEastern);

        builder.Property(x => x.RunCompletedAtEastern);

        builder.Property(x => x.FailureReason)
               .HasMaxLength(1000);

        #endregion

        #region ========== *** Relationships *** ==========

        builder.HasOne(x => x.SupersededByRun)
               .WithMany(x => x.SupersededRuns)
               .HasForeignKey(x => x.SupersededByRunId)
               .OnDelete(DeleteBehavior.NoAction);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => new { x.RequestId, x.AppUpdatedAtEastern })
               .HasDatabaseName("IX_sync_run_request_id_app_updated_at_eastern");

        builder.HasIndex(x => new { x.Status, x.AppUpdatedAtEastern })
               .HasDatabaseName("IX_sync_run_status_app_updated_at_eastern");

        builder.HasIndex(x => x.SupersededByRunId)
               .HasDatabaseName("IX_sync_run_superseded_by_run_id");

        // One active (pending/running) run per request.
        builder.HasIndex(x => x.RequestId)
               .IsUnique()
               .HasFilter("[status] IN ('PENDING','RUNNING')")
               .HasDatabaseName("UX_sync_run_request_active");

        #endregion
    }
}
