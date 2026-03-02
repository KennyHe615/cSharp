using Application.Contracts.ExternalApis.Genesys.Enums;

using AutoMapper;

using Infrastructure.ExternalApis.Providers.Genesys.Enums;
using Infrastructure.Persistence.Mappers.Shared;

using Xunit;


namespace tests.Unit.Infrastructure.Persistence.Mappers.Shared;

public sealed class EnumMappingProfileTests
{
    private readonly IMapper _mapper;

    public EnumMappingProfileTests()
    {
        MapperConfiguration config = new MapperConfiguration(cfg => cfg.AddProfile<EnumMappingProfile>());
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Profile_Configuration_IsValid()
    {
        MapperConfiguration config = new MapperConfiguration(cfg => cfg.AddProfile<EnumMappingProfile>());
        config.AssertConfigurationIsValid();
    }

    [Theory]
    [InlineData(StateKind.Active, State.Active)]
    [InlineData(StateKind.Deleted, State.Deleted)]
    public void Map_StateKind_To_State(StateKind source, State expected)
    {
        State actual = _mapper.Map<State>(source);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(PresenceTypeKind.System, PresenceType.System)]
    [InlineData(PresenceTypeKind.User, PresenceType.User)]
    public void Map_PresenceTypeKind_To_PresenceType(PresenceTypeKind source, PresenceType expected)
    {
        PresenceType actual = _mapper.Map<PresenceType>(source);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(SystemPresenceKind.Available, SystemPresence.Available)]
    [InlineData(SystemPresenceKind.Busy, SystemPresence.Busy)]
    public void Map_SystemPresenceKind_To_SystemPresence(SystemPresenceKind source, SystemPresence expected)
    {
        SystemPresence actual = _mapper.Map<SystemPresence>(source);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(GroupTypeKind.Official, GroupType.Official)]
    [InlineData(GroupTypeKind.Social, GroupType.Social)]
    public void Map_GroupTypeKind_To_GroupType(GroupTypeKind source, GroupType expected)
    {
        GroupType actual = _mapper.Map<GroupType>(source);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(VisibilityKind.Public, Visibility.Public)]
    [InlineData(VisibilityKind.Members, Visibility.Members)]
    public void Map_VisibilityKind_To_Visibility(VisibilityKind source, Visibility expected)
    {
        Visibility actual = _mapper.Map<Visibility>(source);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(RoutingStatusKind.Idle, RoutingStatus.Idle)]
    [InlineData(RoutingStatusKind.Interacting, RoutingStatus.Interacting)]
    public void Map_RoutingStatusKind_To_RoutingStatus(RoutingStatusKind source, RoutingStatus expected)
    {
        RoutingStatus actual = _mapper.Map<RoutingStatus>(source);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Map_Nullable_StateKind_Null_To_State_Null()
    {
        StateKind? source = null;

        State? actual = _mapper.Map<State?>(source);

        Assert.Null(actual);
    }

    [Fact]
    public void Map_Nullable_PresenceTypeKind_Null_To_PresenceType_Null()
    {
        PresenceTypeKind? source = null;

        PresenceType? actual = _mapper.Map<PresenceType?>(source);

        Assert.Null(actual);
    }

    [Fact]
    public void Map_Nullable_SystemPresenceKind_Null_To_SystemPresence_Null()
    {
        SystemPresenceKind? source = null;

        SystemPresence? actual = _mapper.Map<SystemPresence?>(source);

        Assert.Null(actual);
    }

    [Fact]
    public void Map_Nullable_GroupTypeKind_Null_To_GroupType_Null()
    {
        GroupTypeKind? source = null;

        GroupType? actual = _mapper.Map<GroupType?>(source);

        Assert.Null(actual);
    }

    [Fact]
    public void Map_Nullable_VisibilityKind_Null_To_Visibility_Null()
    {
        VisibilityKind? source = null;

        Visibility? actual = _mapper.Map<Visibility?>(source);

        Assert.Null(actual);
    }

    [Fact]
    public void Map_Nullable_RoutingStatusKind_Null_To_RoutingStatus_Null()
    {
        RoutingStatusKind? source = null;

        RoutingStatus? actual = _mapper.Map<RoutingStatus?>(source);

        Assert.Null(actual);
    }

    [Theory]
    [InlineData(StateKind.Active, State.Active)]
    [InlineData(StateKind.Deleted, State.Deleted)]
    public void Map_Nullable_StateKind_Value_To_State_Value(StateKind source, State expected)
    {
        StateKind? nullableSource = source;

        State? actual = _mapper.Map<State?>(nullableSource);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(PresenceTypeKind.System, PresenceType.System)]
    [InlineData(PresenceTypeKind.User, PresenceType.User)]
    public void Map_Nullable_PresenceTypeKind_Value_To_PresenceType_Value(
        PresenceTypeKind source,
        PresenceType expected)
    {
        PresenceTypeKind? nullableSource = source;

        PresenceType? actual = _mapper.Map<PresenceType?>(nullableSource);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(SystemPresenceKind.Available, SystemPresence.Available)]
    [InlineData(SystemPresenceKind.Busy, SystemPresence.Busy)]
    public void Map_Nullable_SystemPresenceKind_Value_To_SystemPresence_Value(SystemPresenceKind source,
                                                                              SystemPresence expected)
    {
        SystemPresenceKind? nullableSource = source;

        SystemPresence? actual = _mapper.Map<SystemPresence?>(nullableSource);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(GroupTypeKind.Official, GroupType.Official)]
    [InlineData(GroupTypeKind.Social, GroupType.Social)]
    public void Map_Nullable_GroupTypeKind_Value_To_GroupType_Value(GroupTypeKind source, GroupType expected)
    {
        GroupTypeKind? nullableSource = source;

        GroupType? actual = _mapper.Map<GroupType?>(nullableSource);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(VisibilityKind.Public, Visibility.Public)]
    [InlineData(VisibilityKind.Owners, Visibility.Owners)]
    public void Map_Nullable_VisibilityKind_Value_To_Visibility_Value(VisibilityKind source, Visibility expected)
    {
        VisibilityKind? nullableSource = source;

        Visibility? actual = _mapper.Map<Visibility?>(nullableSource);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(RoutingStatusKind.Idle, RoutingStatus.Idle)]
    [InlineData(RoutingStatusKind.Interacting, RoutingStatus.Interacting)]
    public void Map_Nullable_RoutingStatusKind_Value_To_RoutingStatus_Value(RoutingStatusKind source,
                                                                            RoutingStatus expected)
    {
        RoutingStatusKind? nullableSource = source;

        RoutingStatus? actual = _mapper.Map<RoutingStatus?>(nullableSource);

        Assert.Equal(expected, actual);
    }
}
