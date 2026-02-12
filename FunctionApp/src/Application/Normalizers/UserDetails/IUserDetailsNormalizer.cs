using Application.Contracts.UserDetails;
using Application.Dtos.UserDetails;


namespace Application.Normalizers.UserDetails;

/// <summary>
/// Defines a normalizer for converting user details response data into DTOs.
/// </summary>
public interface IUserDetailsNormalizer
{
    /// <summary>
    /// Normalizes user details into primary presence and routing status DTOs.
    /// </summary>
    /// <param name="details">The list of user detail responses to normalize.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description><c>PrimaryPresenceDto</c>: List of primary presence DTOs</description></item>
    /// <item><description><c>RoutingStatusDto</c>: List of routing status DTOs</description></item>
    /// </list>
    /// </returns>
    public (List<PrimaryPresenceDto> PrimaryPresenceDtos, List<RoutingStatusDto> RoutingStatusDtos) Normalize(
        List<UserDetailResponse> details);
}
