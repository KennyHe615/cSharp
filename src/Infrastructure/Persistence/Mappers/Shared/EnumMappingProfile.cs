using Application.Enums;

using AutoMapper;

using Infrastructure.ExternalApis.Genesys.Models.Enums;


namespace Infrastructure.Persistence.Mappers.Shared;

public sealed class EnumMappingProfile : Profile
{
    public EnumMappingProfile()
    {
        CreateMap<StateKind, State>()
           .ConvertUsing(src => (State)(int)src);
        CreateMap<StateKind?, State?>()
           .ConvertUsing(src => src.HasValue ? (State)(int)src.Value : null);

        CreateMap<PresenceTypeKind, PresenceType>()
           .ConvertUsing(src => (PresenceType)(int)src);
        CreateMap<PresenceTypeKind?, PresenceType?>()
           .ConvertUsing(src => src.HasValue ? (PresenceType)(int)src.Value : null);

        CreateMap<SystemPresenceKind, SystemPresence>()
           .ConvertUsing(src => (SystemPresence)(int)src);
        CreateMap<SystemPresenceKind?, SystemPresence?>()
           .ConvertUsing(src => src.HasValue ? (SystemPresence)(int)src.Value : null);

        CreateMap<GroupTypeKind, GroupType>()
           .ConvertUsing(src => (GroupType)(int)src);
        CreateMap<GroupTypeKind?, GroupType?>()
           .ConvertUsing(src => src.HasValue ? (GroupType)(int)src.Value : null);

        CreateMap<VisibilityKind, Visibility>()
           .ConvertUsing(src => (Visibility)(int)src);
        CreateMap<VisibilityKind?, Visibility?>()
           .ConvertUsing(src => src.HasValue ? (Visibility)(int)src.Value : null);

        CreateMap<RoutingStatusKind, RoutingStatus>()
           .ConvertUsing(src => (RoutingStatus)(int)src);
        CreateMap<RoutingStatusKind?, RoutingStatus?>()
           .ConvertUsing(src => src.HasValue ? (RoutingStatus)(int)src.Value : null);
    }
}
