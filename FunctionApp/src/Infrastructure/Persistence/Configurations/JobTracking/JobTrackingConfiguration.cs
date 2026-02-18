using Application.Common.Enums;

using Infrastructure.Persistence.Entities.JobTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Shared.Extensions;


namespace Infrastructure.Persistence.Configurations.JobTracking;

public class JobTrackingConfiguration : IEntityTypeConfiguration<JobTrackingEntity>
{
    public void Configure(EntityTypeBuilder<JobTrackingEntity> builder)
    {
        #region ========== *** Converters *** ==========

        ValueConverter<SyncCategory, string> categoryConverter = new(toProvider => toProvider.WriteEnumSnakeUpper(),
                                                                     fromProvider =>
                                                                         fromProvider.ReadEnum<SyncCategory>());

        #endregion

        builder.ToTable("sync_job_tracking", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Category).HasConversion(categoryConverter).HasMaxLength(50);

        builder.Property(x => x.Interval).HasMaxLength(50);

        builder.Property(x => x.PageNumber);

        builder.Property(x => x.JobId).HasMaxLength(100);

        builder.Property(x => x.IsIncrementalCompleted).HasDefaultValue(false);

        builder.Property(x => x.IsRecoveryCompleted).HasDefaultValue(false);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.Category);

        builder.HasIndex(x => x.IsIncrementalCompleted);

        builder.HasIndex(x => x.IsRecoveryCompleted);

        #endregion
    }
}
