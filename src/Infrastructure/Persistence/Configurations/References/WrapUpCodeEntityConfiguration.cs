using Infrastructure.Persistence.Entities.References;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.References;

public sealed class WrapUpCodeEntityConfiguration : IEntityTypeConfiguration<WrapUpCode>
{
    public void Configure(EntityTypeBuilder<WrapUpCode> builder)
    {
        builder.ToTable("wrap_up_codes", "ref");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Id)
               .IsRequired();

        builder.Property(x => x.Name)
               .HasMaxLength(255);

        builder.Property(x => x.DivisionId)
               .HasMaxLength(36);

        builder.Property(x => x.DivisionName)
               .HasMaxLength(255);

        builder.Property(x => x.DateCreated)
               .HasColumnType("datetimeoffset(0)");

        builder.Property(x => x.DateModified)
               .HasColumnType("datetimeoffset(0)");

        builder.Property(x => x.CreatedBy)
               .HasMaxLength(36);

        builder.Property(x => x.ModifiedBy)
               .HasMaxLength(36);

        builder.Property(x => x.State)
               .HasMaxLength(8);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.Name);

        builder.HasIndex(x => x.DivisionId);

        #endregion
    }
}
