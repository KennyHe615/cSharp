using Application.Contracts.ExternalApis.Genesys.UsersDetails;
using Application.DTOs.UserDetails;


namespace Application.Abstractions.Normalization;

/// <summary>
/// Normalizes raw external users-details contracts into persistence-ready DTO sets.
/// </summary>
public interface IUsersDetailsNormalizer
{
    public (IReadOnlyCollection<PrimaryPresenceDto> PrimaryPresenceDtos, IReadOnlyCollection<RoutingStatusDto>
        RoutingStatusDtos) NormalizeUsersDetails(UsersDetailsRawContract response);
}
