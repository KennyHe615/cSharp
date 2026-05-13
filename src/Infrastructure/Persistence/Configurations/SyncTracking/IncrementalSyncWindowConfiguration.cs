using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.SyncTracking;

/// <summary>
/// Entity Framework configuration for durable incremental scheduling cursors.
/// </summary>
public sealed class IncrementalSyncWindowConfiguration : IEntityTypeConfiguration<IncrementalSyncWindowEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IncrementalSyncWindowEntity> builder)
    {
        builder.ToTable("incremental_sync_window", "dbo");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Id)
               .ValueGeneratedOnAdd();

        builder.Property(x => x.Category)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(x => x.NextIntervalStartUtc)
               .IsRequired()
               .HasColumnType("datetimeoffset(3)");

        builder.Property(x => x.LastReservedStartUtc)
               .HasColumnType("datetimeoffset(3)");

        builder.Property(x => x.LastReservedEndUtc)
               .HasColumnType("datetimeoffset(3)");

        builder.Property(x => x.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.Category)
               .IsUnique()
               .HasDatabaseName("UX_incremental_sync_window_category");

        builder.HasIndex(x => x.AppUpdatedAtEastern)
               .HasDatabaseName("IX_incremental_sync_window_app_updated_at_eastern");

        #endregion
    }
}
