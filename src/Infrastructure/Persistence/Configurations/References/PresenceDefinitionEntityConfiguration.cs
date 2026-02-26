using Infrastructure.Persistence.Entities.References;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Persistence.Configurations.References;

public sealed class PresenceDefinitionEntityConfiguration : IEntityTypeConfiguration<PresenceDefinition>
{
    public void Configure(EntityTypeBuilder<PresenceDefinition> builder)
    {
        builder.ToTable("presence_definitions", "ref");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Id)
               .IsRequired();

        builder.Property(x => x.Type)
               .HasMaxLength(6);

        builder.Property(x => x.LanguageLabel)
               .HasMaxLength(255);

        builder.Property(x => x.SystemPresence)
               .HasMaxLength(9);

        builder.Property(x => x.DivisionId)
               .HasMaxLength(36);

        builder.Property(x => x.Deactivated);

        #endregion
    }
}
