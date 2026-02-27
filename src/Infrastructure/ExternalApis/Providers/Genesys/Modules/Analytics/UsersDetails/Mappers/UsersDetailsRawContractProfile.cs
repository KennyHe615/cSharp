using Application.Contracts.ExternalApis.Genesys.UsersDetails;

using AutoMapper;

using Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.UsersDetails.Contracts;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.UsersDetails.Mappers;

public sealed class UsersDetailsRawContractProfile : Profile
{
    public UsersDetailsRawContractProfile()
    {
        CreateMap<UsersDetailsResponse, UsersDetailsRawContract>();

        CreateMap<UserDetailsResponse, UserDetailsRawContract>();

        CreateMap<PrimaryPresenceResponse, PrimaryPresenceRawContract>();

        CreateMap<RoutingStatusResponse, RoutingStatusRawContract>();
    }
}
