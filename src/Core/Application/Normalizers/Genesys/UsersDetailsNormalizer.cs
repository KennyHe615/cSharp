using Application.Abstractions.Normalization;
using Application.Contracts.ExternalApis.Genesys.UsersDetails;
using Application.DTOs.UsersDetails;

using SharedKernel.Time;


namespace Application.Normalizers.Genesys;

/// <summary>
/// Applies normalization rules for users-details payload into persistence DTOs.
/// </summary>
public sealed class UsersDetailsNormalizer : IUsersDetailsNormalizer
{
    public (IReadOnlyCollection<PrimaryPresenceDto> PrimaryPresenceDtos, IReadOnlyCollection<RoutingStatusDto>
            RoutingStatusDtos) NormalizeUsersDetails(UsersDetailsRawContract response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.UserDetails.Count == 0) return ([], []);

        List<PrimaryPresenceDto> primary = [];
        List<RoutingStatusDto> routing = [];

        foreach (UserDetailsRawContract user in response.UserDetails)
        {
            Guid userId = user.UserId;

            if (user.PrimaryPresence is { Count: > 0 })
            {
                primary.AddRange(user.PrimaryPresence.Select(item => new PrimaryPresenceDto
                                                                     {
                                                                         UserId = userId,
                                                                         StartTime = item.StartTime,
                                                                         EndTime = item.EndTime,
                                                                         DurationInSeconds =
                                                                                 item.StartTime
                                                                                        .CalculateDurationTo(item
                                                                                                .EndTime),
                                                                         SystemPresence = item.SystemPresence,
                                                                         OrganizationPresenceId =
                                                                                 item.OrganizationPresenceId
                                                                     }));
            }

            if (user.RoutingStatus is { Count: > 0 })
            {
                routing.AddRange(user.RoutingStatus.Select(item => new RoutingStatusDto
                                                                   {
                                                                       UserId = userId,
                                                                       StartTime = item.StartTime,
                                                                       EndTime = item.EndTime,
                                                                       DurationInSeconds =
                                                                               item.StartTime
                                                                                      .CalculateDurationTo(item
                                                                                              .EndTime),
                                                                       RoutingStatus = item.RoutingStatus
                                                                   }));
            }
        }

        return (primary, routing);
    }
}
