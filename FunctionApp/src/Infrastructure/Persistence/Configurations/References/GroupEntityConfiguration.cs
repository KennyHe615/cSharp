using Application.Contracts.Enums;

using Infrastructure.Persistence.Entities.References;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Shared.Extensions;


namespace Infrastructure.Persistence.Configurations.References;

public class GroupEntityConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        #region ========== *** Converters *** ==========

        ValueConverter<State, string> stateConverter = new(toProvider => toProvider.WriteEnumSnakeUpper(),
                                                           fromProvider => fromProvider.ReadEnum<State>());

        ValueConverter<GroupType, string> groupTypeConverter = new(toProvider => toProvider.WriteEnumSnakeUpper(),
                                                                   fromProvider => fromProvider.ReadEnum<GroupType>());

        ValueConverter<GroupVisibility, string> groupVisibilityConverter =
            new(toProvider => toProvider.WriteEnumSnakeUpper(),
                fromProvider => fromProvider.ReadEnum<GroupVisibility>());

        #endregion

        builder.ToTable("groups", "ref");

        builder.HasKey(x => x.Id);

        #region ========== *** Properties *** ==========

        builder.Property(x => x.Name).HasMaxLength(255);

        builder.Property(x => x.Description).HasMaxLength(255);

        builder.Property(x => x.DateModified).HasColumnType("datetimeoffset(0)");

        builder.Property(x => x.MemberCount);

        builder.Property(x => x.State).HasConversion(stateConverter).HasMaxLength(8);

        builder.Property(x => x.Version);

        builder.Property(x => x.Type).HasConversion(groupTypeConverter).HasMaxLength(8);

        builder.Property(x => x.RulesVisible);

        builder.Property(x => x.Visibility).HasConversion(groupVisibilityConverter).HasMaxLength(7);

        builder.Property(x => x.ChatJabberId).HasMaxLength(255);

        builder.Property(x => x.RolesEnabled);

        builder.Property(x => x.IncludeOwners);

        #endregion

        #region ========== *** Non-Clustered Indexes *** ==========

        builder.HasIndex(x => x.Name);

        #endregion
    }
}
