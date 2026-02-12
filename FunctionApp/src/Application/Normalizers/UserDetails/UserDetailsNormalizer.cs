using Application.Contracts.UserDetails;
using Application.Dtos.UserDetails;

using Shared.Time;


namespace Application.Normalizers.UserDetails;

/// <summary>
/// Normalizes user details response data into DTOs for persistence.
/// </summary>
public sealed class UserDetailsNormalizer : IUserDetailsNormalizer
{
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="details"/> is <c>null</c>.</exception>
    public (List<PrimaryPresenceDto> PrimaryPresenceDtos, List<RoutingStatusDto> RoutingStatusDtos) Normalize(
        List<UserDetailResponse> details)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (details.Count == 0) return ([], []);

        List<PrimaryPresenceDto> primary = [];
        List<RoutingStatusDto> routing = [];

        foreach (UserDetailResponse detail in details)
        {
            Guid userId = detail.UserId;

            if (detail.PrimaryPresence is { Count: > 0 })
            {
                primary.AddRange(detail.PrimaryPresence.Select(item => new PrimaryPresenceDto
                                                                       {
                                                                           UserId = userId,
                                                                           StartTime = item.StartTime,
                                                                           EndTime = item.EndTime,
                                                                           DurationInSeconds =
                                                                               DateTimeResolver.CalculateDuration(
                                                                                   item.StartTime,
                                                                                   item.EndTime),
                                                                           SystemPresence = item.SystemPresence,
                                                                           OrganizationPresenceId =
                                                                               item.OrganizationPresenceId
                                                                       }));
            }

            if (detail.RoutingStatus is { Count: > 0 })
            {
                routing.AddRange(detail.RoutingStatus.Select(item => new RoutingStatusDto
                                                                     {
                                                                         UserId = userId,
                                                                         StartTime = item.StartTime,
                                                                         EndTime = item.EndTime,
                                                                         DurationInSeconds =
                                                                             DateTimeResolver.CalculateDuration(
                                                                                 item.StartTime,
                                                                                 item.EndTime),
                                                                         RoutingStatus = item.RoutingStatus
                                                                     }));
            }
        }

        return (primary, routing);
    }
}
