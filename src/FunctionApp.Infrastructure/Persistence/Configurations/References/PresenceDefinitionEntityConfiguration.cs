using FunctionApp.Domain.Entities.References;
using FunctionApp.Domain.Enums.References;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace FunctionApp.Infrastructure.Persistence.Configurations.References;

public class PresenceDefinitionEntityConfiguration : IEntityTypeConfiguration<PresenceDefinition>
{
    public void Configure(EntityTypeBuilder<PresenceDefinition> builder)
    {
        builder.ToTable("presence_definitions", "ref");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Type)
               .HasConversion(v => v.HasValue ? v.Value.ToString().ToLowerInvariant() : null,
                              v => v != null ? Enum.Parse<PresenceType>(v, true) : null)
               .HasMaxLength(6);

        builder.Property(x => x.LanguageLabel).HasMaxLength(255);

        builder.Property(x => x.SystemPresence)
               .HasConversion(v => v.HasValue ? v.Value.ToString().ToLowerInvariant() : null,
                              v => v != null ? Enum.Parse<SystemPresence>(v, true) : null)
               .HasMaxLength(9);

        builder.Property(x => x.DivisionId).HasMaxLength(36);

        builder.Property(x => x.Deactivated);

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

        builder.HasIndex(x => x.AppUpdatedAt);

        #endregion
    }
}
