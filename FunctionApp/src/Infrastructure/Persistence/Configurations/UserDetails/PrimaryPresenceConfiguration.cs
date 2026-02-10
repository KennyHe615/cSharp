using Application.Contracts.Enums;

using Infrastructure.Persistence.Entities.UserDetails;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Shared.Extensions;


namespace Infrastructure.Persistence.Configurations.UserDetails;

public class PrimaryPresenceConfiguration : IEntityTypeConfiguration<PrimaryPresenceEntity>
{
    public void Configure(EntityTypeBuilder<PrimaryPresenceEntity> builder)
    {
        #region ========== *** Converters *** ==========

        ValueConverter<SystemPresence, string> converter = new(toProvider => toProvider.WriteEnumSnakeUpper(),
                                                               fromProvider => fromProvider.ReadEnum<SystemPresence>());

        #endregion

        builder.ToTable("user_details_primary_presence_stg", "dbo");

        builder.HasKey(x => new
                            {
                                x.UserId,
                                x.StartTime
                            });

        #region ========== *** Properties *** ==========

        builder.Property(x => x.StartTime).HasColumnType("datetimeoffset(3)");

        builder.Property(x => x.EndTime).HasColumnType("datetimeoffset(3)");

        builder.Property(x => x.DurationInSeconds).HasColumnType("bigint");

        builder.Property(x => x.SystemPresence).HasConversion(converter).HasMaxLength(9);

        builder.Property(x => x.OrganizationPresenceId).HasMaxLength(255);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.SystemPresence);

        #endregion
    }
}
