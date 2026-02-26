using Infrastructure.Persistence.Entities.JobTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.JobTracking;

public sealed class JobTrackingConfiguration : IEntityTypeConfiguration<JobTrackingEntity>
{
    public void Configure(EntityTypeBuilder<JobTrackingEntity> builder)
    {
        builder.ToTable("sync_job_tracking", "dbo");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Id)
               .ValueGeneratedOnAdd();

        builder.Property(x => x.DataType)
               .IsRequired()
               .HasColumnName("category")
               .HasMaxLength(50);

        builder.Property(x => x.Interval)
               .HasMaxLength(50);

        builder.Property(x => x.PageNumber);

        builder.Property(x => x.JobId)
               .HasMaxLength(100);

        builder.Property(x => x.IsIncrementalCompleted)
               .HasDefaultValue(false);

        builder.Property(x => x.IsRecoveryCompleted)
               .HasDefaultValue(false);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.DataType)
               .HasDatabaseName("ix_sync_job_tracking_category");

        builder.HasIndex(x => x.IsIncrementalCompleted);

        builder.HasIndex(x => x.IsRecoveryCompleted);

        #endregion
    }
}
