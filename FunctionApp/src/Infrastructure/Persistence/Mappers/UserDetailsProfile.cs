using Application.Contracts.Enums;
using Application.Dtos.UserDetails;

using AutoMapper;

using Infrastructure.Persistence.Entities.UserDetails;
using Infrastructure.Persistence.Mappers.Shared;


namespace Infrastructure.Persistence.Mappers;

public class UserDetailsProfile : Profile
{
    public UserDetailsProfile()
    {
        CreateMap<PrimaryPresenceDto, PrimaryPresenceEntity>();

        CreateMap<RoutingStatusDto, RoutingStatusEntity>();

        CreateMap<string, RoutingStatus>().ConvertUsing<StringToEnumConverter<RoutingStatus>>();
        CreateMap<RoutingStatus, string>().ConvertUsing<EnumToStringSnakeUpperConverter<RoutingStatus>>();

        CreateMap<string, SystemPresence>().ConvertUsing<StringToEnumConverter<SystemPresence>>();
        CreateMap<SystemPresence, string>().ConvertUsing<EnumToStringSnakeUpperConverter<SystemPresence>>();
    }
}
