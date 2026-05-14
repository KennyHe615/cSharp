using Infrastructure.Persistence.Entities.UserDetails;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.UserDetails;

public sealed class PrimaryPresenceConfiguration : IEntityTypeConfiguration<PrimaryPresenceEntity>
{
    public void Configure(EntityTypeBuilder<PrimaryPresenceEntity> builder)
    {
        builder.ToTable("users_details_primary_presence_stg", "dbo");

        builder.HasKey(x => new
                            {
                                x.UserId,
                                x.StartTimeUtc
                            });

        #region ========== *** Properties *** ==========

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

        builder.Property(x => x.SystemPresence)
               .IsRequired()
               .HasMaxLength(9);

        builder.Property(x => x.OrganizationPresenceId)
               .HasMaxLength(255);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex("StartDateEastern", nameof(PrimaryPresenceEntity.SystemPresence));

        #endregion
    }
}
