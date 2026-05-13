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

        // External/client identifier. Internal joins still use bigint Id.
        builder.Property(x => x.PublicId)
               .IsRequired()
               .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.Category)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(x => x.Mode)
               .IsRequired()
               .HasMaxLength(20);

        // Request-level lifecycle used by recovery resolve semantics.
        builder.Property(x => x.Status)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(x => x.ReopenCount)
               .IsRequired()
               .HasDefaultValue(0);

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

        builder.HasIndex(x => x.PublicId)
               .IsUnique()
               .HasDatabaseName("UX_sync_request_public_id");

        // Full mode keeps one logical request row per scope.
        builder.HasIndex(x => x.ScopeKey)
               .IsUnique()
               .HasFilter("[mode] = 'FULL'")
               .HasDatabaseName("UX_sync_request_scope_key_full");

        // Incremental mode keeps one logical request row per scope.
        builder.HasIndex(x => x.ScopeKey)
               .IsUnique()
               .HasFilter("[mode] = 'INCREMENTAL'")
               .HasDatabaseName("UX_sync_request_scope_key_incremental");

        // Recovery mode allows history, but only one active request per scope at a time.
        builder.HasIndex(x => x.ScopeKey)
               .IsUnique()
               .HasFilter("[mode] = 'RECOVERY' AND [status] IN ('PENDING','RUNNING')")
               .HasDatabaseName("UX_sync_request_scope_key_recovery_active");

        // Supports latest-by-scope resolution logic in recovery paths.
        builder.HasIndex(x => new { x.Mode, x.ScopeKey, x.AppUpdatedAtEastern })
               .HasDatabaseName("IX_sync_request_mode_scope_key_app_updated_at_eastern");

        builder.HasIndex(x => new { x.Category, x.Mode, x.AppUpdatedAtEastern })
               .HasDatabaseName("IX_sync_request_category_mode_app_updated_at_eastern");

        builder.HasIndex(x => x.CurrentRunId)
               .HasDatabaseName("IX_sync_request_current_run_id");

        #endregion
    }
}
