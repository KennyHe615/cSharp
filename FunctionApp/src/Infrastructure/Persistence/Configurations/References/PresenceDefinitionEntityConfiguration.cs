using Application.Contracts.Enums;

using Infrastructure.Persistence.Entities.References;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Shared.Extensions;


namespace Infrastructure.Persistence.Configurations.References;

public class PresenceDefinitionEntityConfiguration : IEntityTypeConfiguration<PresenceDefinition>
{
    public void Configure(EntityTypeBuilder<PresenceDefinition> builder)
    {
        #region ========== *** Converters *** ==========

        ValueConverter<PresenceType, string> presenceTypeConverter = new(toProvider => toProvider.WriteEnumSnakeUpper(),
                                                                         fromProvider =>
                                                                             fromProvider.ReadEnum<PresenceType>());

        ValueConverter<SystemPresence, string> systemPresenceConverter =
            new(toProvider => toProvider.WriteEnumSnakeUpper(),
                fromProvider => fromProvider.ReadEnum<SystemPresence>());

        #endregion

        builder.ToTable("presence_definitions", "ref");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Type).HasConversion(presenceTypeConverter).HasMaxLength(6);

        builder.Property(x => x.LanguageLabel).HasMaxLength(255);

        builder.Property(x => x.SystemPresence).HasConversion(systemPresenceConverter).HasMaxLength(9);

        builder.Property(x => x.DivisionId).HasMaxLength(36);

        builder.Property(x => x.Deactivated);

        #endregion
    }
}
