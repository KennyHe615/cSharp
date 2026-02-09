using Application.Contracts.Enums;

using Infrastructure.Persistence.Entities.References;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Shared.Extensions;


namespace Infrastructure.Persistence.Configurations.References;

public class SkillEntityConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        #region ========== *** Converters *** ==========

        ValueConverter<State, string> converter = new(toProvider => toProvider.WriteEnumSnakeUpper(),
                                                      fromProvider => fromProvider.ReadEnum<State>());

        #endregion

        builder.ToTable("skills", "ref");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Name).HasMaxLength(255);

        builder.Property(x => x.Version).HasMaxLength(8);

        builder.Property(x => x.State).HasConversion(converter).HasMaxLength(8);

        builder.Property(x => x.DateModified).HasColumnType("datetimeoffset(0)");

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.Name);

        #endregion
    }
}
