using Application.Enums;

using Infrastructure.Persistence.Entities.References;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.References;

public class SkillEntityConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills", "ref");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Name).HasMaxLength(255);

        builder.Property(x => x.Version).HasMaxLength(8);

        builder.Property(x => x.State)
               .HasConversion(v => v.HasValue ? v.Value.ToString().ToLowerInvariant() : null,
                              v => v != null ? Enum.Parse<State>(v, true) : null)
               .HasMaxLength(8);

        builder.Property(x => x.DateModified).HasColumnType("datetimeoffset(0)");

        builder.Property(x => x.AppCreatedAt)
               .IsRequired()
               .HasColumnType("datetimeoffset(0)")
               .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(x => x.AppUpdatedAt)
               .IsRequired()
               .HasColumnType("datetimeoffset(0)")
               .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.AppUpdatedAt);

        #endregion
    }
}
