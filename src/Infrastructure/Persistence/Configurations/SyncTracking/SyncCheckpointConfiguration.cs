using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.SyncTracking;

public sealed class SyncCheckpointConfiguration : IEntityTypeConfiguration<SyncCheckpointEntity>
{
    public void Configure(EntityTypeBuilder<SyncCheckpointEntity> builder)
    {
        builder.ToTable("sync_checkpoint", "dbo");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Id)
               .ValueGeneratedOnAdd();

        builder.Property(x => x.RunId)
               .IsRequired();

        builder.Property(x => x.Step)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(x => x.Cursor)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(x => x.Status)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(x => x.FailureReason)
               .HasMaxLength(1000);

        #endregion

        #region ========== *** Relationships *** ==========

        builder.HasOne(x => x.Run)
               .WithMany(x => x.Checkpoints)
               .HasForeignKey(x => x.RunId)
               .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => new { x.RunId, x.Step, x.Cursor })
               .IsUnique()
               .HasDatabaseName("UX_sync_checkpoint_run_step_cursor");

        builder.HasIndex(x => new { x.RunId, x.Status, x.AppUpdatedAt })
               .HasDatabaseName("IX_sync_checkpoint_run_status_app_updated_at");

        builder.HasIndex(x => x.AppUpdatedAt)
               .HasDatabaseName("IX_sync_checkpoint_app_updated_at");

        #endregion
    }
}
