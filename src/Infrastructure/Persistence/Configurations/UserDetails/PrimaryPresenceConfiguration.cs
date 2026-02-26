using Infrastructure.Persistence.Entities.UserDetails;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.UserDetails;

public sealed class PrimaryPresenceConfiguration : IEntityTypeConfiguration<PrimaryPresenceEntity>
{
    public void Configure(EntityTypeBuilder<PrimaryPresenceEntity> builder)
    {
        builder.ToTable("user_details_primary_presence_stg", "dbo");

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

        builder.Property(x => x.SystemPresence)
               .IsRequired()
               .HasMaxLength(9);

        builder.Property(x => x.OrganizationPresenceId)
               .HasMaxLength(255);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.SystemPresence);

        #endregion
    }
}
