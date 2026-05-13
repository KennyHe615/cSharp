using Infrastructure.Persistence.Entities.UserDetails;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.UserDetails;

public sealed class RoutingStatusConfiguration : IEntityTypeConfiguration<RoutingStatusEntity>
{
    public void Configure(EntityTypeBuilder<RoutingStatusEntity> builder)
    {
        builder.ToTable("users_details_routing_status_stg", "dbo");

        builder.HasKey(x => new
                            {
                                x.UserId,
                                x.StartTimeUtc
                            });

        #region ========== *** Properties *** ==========

        builder.Property(x => x.UserId)
               .IsRequired();

        builder.Property(x => x.StartTimeUtc)
               .IsRequired()
               .HasColumnType("datetimeoffset(3)");

        builder.Property(x => x.EndTimeUtc)
               .HasColumnType("datetimeoffset(3)");

        builder.Property<long?>("DurationInSeconds")
               .HasColumnType("bigint")
               .HasComputedColumnSql("CASE WHEN [end_time_utc] IS NULL THEN NULL ELSE DATEDIFF_BIG(SECOND, [start_time_utc], [end_time_utc]) END",
                                     true);

        builder.Property(x => x.StartTimeEastern)
               .IsRequired()
               .HasColumnType("datetimeoffset(0)");

        builder.Property<DateOnly>("StartDateEastern")
               .HasColumnType("date")
               .HasComputedColumnSql("CAST([start_time_eastern] AS DATE)", true);

        builder.Property(x => x.RoutingStatus)
               .IsRequired()
               .HasMaxLength(15);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex("StartDateEastern", nameof(RoutingStatusEntity.RoutingStatus));

        #endregion
    }
}
