using FunctionApp.Domain.Entities.References;
using FunctionApp.Domain.Enums.References;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace FunctionApp.Infrastructure.Persistence.Configurations.References;

public class SkillEntityConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills", "ref");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(255);

        builder.Property(x => x.Version).HasMaxLength(8);

        // Map State Enum to lowercase strings: "active", "inactive", "deleted"
        builder.Property(x => x.State)
               .HasConversion(v => v.HasValue ? v.Value.ToString().ToLowerInvariant() : null,
                              v => v != null ? Enum.Parse<SkillState>(v, true) : null)
               .HasMaxLength(8);

        // Timestamps mapped to datetimeoffset to preserve the "-04:00" offset requirement
        builder.Property(x => x.DateModified).HasColumnType("datetimeoffset(0)");

        builder.Property(x => x.AppCreatedAt)
               .IsRequired()
               .HasColumnType("datetimeoffset(0)")
               .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(x => x.AppUpdatedAt)
               .IsRequired()
               .HasColumnType("datetimeoffset(0)")
               .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        // Non-clustered indexes (Default for HasIndex in EF Core)
        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.State);
        builder.HasIndex(x => x.AppCreatedAt);
        builder.HasIndex(x => x.AppUpdatedAt);
    }
}
