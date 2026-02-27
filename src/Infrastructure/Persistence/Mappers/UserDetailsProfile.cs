using Application.DTOs.UserDetails;

using AutoMapper;

using Infrastructure.Persistence.Entities.UserDetails;


namespace Infrastructure.Persistence.Mappers;

public sealed class UserDetailsProfile : Profile
{
    public UserDetailsProfile()
    {
        CreateMap<PrimaryPresenceDto, PrimaryPresenceEntity>();

        CreateMap<RoutingStatusDto, RoutingStatusEntity>();
    }
}
