using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.SyncTracking;

/// <summary>
/// Entity Framework configuration for claimable sync run items.
/// </summary>
public sealed class SyncRunItemConfiguration : IEntityTypeConfiguration<SyncRunItemEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncRunItemEntity> builder)
    {
        builder.ToTable("sync_run_item", "dbo");

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
               .WithMany(x => x.RunItems)
               .HasForeignKey(x => x.RunId)
               .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => new { x.RunId, x.Step, x.Cursor })
               .IsUnique()
               .HasDatabaseName("UX_sync_run_item_run_step_cursor");

        builder.HasIndex(x => new { x.RunId, x.Status, x.AppUpdatedAtEastern })
               .HasDatabaseName("IX_sync_run_item_run_status_app_updated_at_eastern");

        builder.HasIndex(x => x.AppUpdatedAtEastern)
               .HasDatabaseName("IX_sync_run_item_app_updated_at_eastern");

        #endregion
    }
}
