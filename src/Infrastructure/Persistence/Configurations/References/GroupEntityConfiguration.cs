using Infrastructure.Persistence.Entities.References;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.References;

public sealed class GroupEntityConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups", "ref");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Id)
               .IsRequired();

        builder.Property(x => x.Name)
               .HasMaxLength(255);

        builder.Property(x => x.Description)
               .HasMaxLength(255);

        builder.Property(x => x.DateModified)
               .HasColumnType("datetimeoffset(0)");

        builder.Property(x => x.MemberCount);

        builder.Property(x => x.State)
               .HasMaxLength(8);

        builder.Property(x => x.Version);

        builder.Property(x => x.Type)
               .HasMaxLength(8);

        builder.Property(x => x.RulesVisible);

        builder.Property(x => x.Visibility)
               .HasMaxLength(7);

        builder.Property(x => x.ChatJabberId)
               .HasMaxLength(255);

        builder.Property(x => x.RolesEnabled);

        builder.Property(x => x.IncludeOwners);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.Name);

        #endregion
    }
}
