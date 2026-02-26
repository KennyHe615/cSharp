using Infrastructure.Persistence.Entities.UserDetails;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.UserDetails;

public sealed class RoutingStatusConfiguration : IEntityTypeConfiguration<RoutingStatusEntity>
{
    public void Configure(EntityTypeBuilder<RoutingStatusEntity> builder)
    {
        builder.ToTable("user_details_routing_status_stg", "dbo");

        builder.HasKey(x => new
                            {
                                x.UserId,
                                x.StartTime
                            });

        #region ========== *** Properties *** ==========

        builder.Property(x => x.UserId)
               .IsRequired();

        builder.Property(x => x.StartTime)
               .IsRequired()
               .HasColumnType("datetimeoffset(3)");

        builder.Property(x => x.EndTime)
               .HasColumnType("datetimeoffset(3)");

        builder.Property(x => x.DurationInSeconds)
               .HasColumnType("bigint");

        builder.Property(x => x.RoutingStatus)
               .IsRequired()
               .HasMaxLength(15);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.RoutingStatus);

        #endregion
    }
}
