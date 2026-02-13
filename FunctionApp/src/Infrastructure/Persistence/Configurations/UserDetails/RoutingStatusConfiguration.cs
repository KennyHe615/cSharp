using Application.Contracts.Enums;

using Infrastructure.Persistence.Entities.UserDetails;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Shared.Extensions;


namespace Infrastructure.Persistence.Configurations.UserDetails;

public class RoutingStatusConfiguration : IEntityTypeConfiguration<RoutingStatusEntity>
{
    public void Configure(EntityTypeBuilder<RoutingStatusEntity> builder)
    {
        #region ========== *** Converters *** ==========

        ValueConverter<RoutingStatus, string> converter = new(toProvider => toProvider.WriteEnumSnakeUpper(),
                                                              fromProvider => fromProvider.ReadEnum<RoutingStatus>());

        #endregion

        builder.ToTable("user_details_routing_status_stg", "dbo");

        builder.HasKey(x => new
                            {
                                x.UserId,
                                x.StartTime
                            });

        #region ========== *** Properties *** ==========

        builder.Property(x => x.StartTime).HasColumnType("datetimeoffset(3)");

        builder.Property(x => x.EndTime).HasColumnType("datetimeoffset(3)");

        builder.Property(x => x.DurationInSeconds).HasColumnType("bigint");

        builder.Property(x => x.RoutingStatus).HasConversion(converter).HasMaxLength(15);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.RoutingStatus);

        #endregion
    }
}
