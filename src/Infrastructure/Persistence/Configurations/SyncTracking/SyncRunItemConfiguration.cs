using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.SyncTracking;

/// <summary>
/// Entity Framework configuration for claimable sync run items.
/// A run item may be keyed either by a generic cursor or by a page number, but not both.
/// </summary>
public sealed class SyncRunItemConfiguration : IEntityTypeConfiguration<SyncRunItemEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncRunItemEntity> builder)
    {
        builder.ToTable("sync_run_item",
                        "dbo",
                        tableBuilder =>
                        {
                            tableBuilder.HasCheckConstraint("CK_sync_run_item_selector_shape",
                                                            "(([page_number] IS NULL AND [cursor] IS NOT NULL) OR ([page_number] IS NOT NULL AND [cursor] IS NULL))");
                        });

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
               .HasMaxLength(200);

        builder.Property(x => x.PageNumber);

        builder.Property(x => x.Status)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(x => x.FailureReason)
               .HasMaxLength(1000);

        builder.Property(x => x.ClaimedBy)
               .HasMaxLength(200);

        builder.Property(x => x.LeaseToken);

        builder.Property(x => x.ClaimedAtEastern)
               .HasColumnType("datetimeoffset(0)");

        builder.Property(x => x.ClaimExpiresAtEastern)
               .HasColumnType("datetimeoffset(0)");

        builder.Property(x => x.AttemptCount)
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(x => x.LastHeartbeatAtEastern)
               .HasColumnType("datetimeoffset(0)");

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
               .HasFilter("[page_number] IS NULL AND [cursor] IS NOT NULL")
               .HasDatabaseName("UX_sync_run_item_run_step_cursor");

        builder.HasIndex(x => new { x.RunId, x.Step, x.PageNumber })
               .IsUnique()
               .HasFilter("[page_number] IS NOT NULL")
               .HasDatabaseName("UX_sync_run_item_run_step_page_number");

        builder.HasIndex(x => new { x.RunId, x.Status, x.AppUpdatedAtEastern })
               .HasDatabaseName("IX_sync_run_item_run_status_app_updated_at_eastern");

        builder.HasIndex(x => new { x.RunId, x.Step, x.Status, x.ClaimExpiresAtEastern, x.PageNumber })
               .HasFilter("[page_number] IS NOT NULL")
               .HasDatabaseName("IX_sync_run_item_run_step_status_claim_exp_page");

        builder.HasIndex(x => new { x.RunId, x.Step, x.Status, x.ClaimExpiresAtEastern, x.Cursor })
               .HasFilter("[page_number] IS NULL AND [cursor] IS NOT NULL")
               .HasDatabaseName("IX_sync_run_item_run_step_status_claim_exp_cursor");

        builder.HasIndex(x => new { x.RunId, x.Step, x.ClaimedBy, x.Status })
               .HasDatabaseName("IX_sync_run_item_run_step_claimed_by_status");

        builder.HasIndex(x => x.LeaseToken)
               .HasDatabaseName("IX_sync_run_item_lease_token");

        builder.HasIndex(x => x.AppUpdatedAtEastern)
               .HasDatabaseName("IX_sync_run_item_app_updated_at_eastern");

        #endregion
    }
}
