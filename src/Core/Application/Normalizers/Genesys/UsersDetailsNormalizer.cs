using Application.Abstractions.Normalization;
using Application.Contracts.ExternalApis.Genesys.UsersDetails;
using Application.DTOs.UsersDetails;

using SharedKernel.Time;


namespace Application.Normalizers.Genesys;

/// <summary>
/// Applies normalization rules for users-details payload into persistence DTOs.
/// </summary>
/// <param name="dateTimeProvider">Time provider used to convert UTC timestamps to Eastern time.</param>
public sealed class UsersDetailsNormalizer(IDateTimeProvider dateTimeProvider) : IUsersDetailsNormalizer
{
    private readonly IDateTimeProvider _dateTimeProvider =
            dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));

    /// <inheritdoc />
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
                primary.AddRange(user.PrimaryPresence.Select(item =>
                                                             {
                                                                 DateTimeOffset startTimeUtc =
                                                                         item.StartTime.NormalizeToUtc();

                                                                 return new PrimaryPresenceDto
                                                                        {
                                                                            UserId = userId,
                                                                            StartTimeUtc = startTimeUtc,
                                                                            EndTimeUtc =
                                                                                    item.EndTime.NormalizeToUtc(),
                                                                            StartTimeEastern =
                                                                                    _dateTimeProvider
                                                                                           .ConvertToEst(startTimeUtc),
                                                                            SystemPresence = item.SystemPresence,
                                                                            OrganizationPresenceId =
                                                                                    item.OrganizationPresenceId
                                                                        };
                                                             }));
            }

            if (user.RoutingStatus is { Count: > 0 })
            {
                routing.AddRange(user.RoutingStatus.Select(item =>
                                                           {
                                                               DateTimeOffset startTimeUtc =
                                                                       item.StartTime.NormalizeToUtc();

                                                               return new RoutingStatusDto
                                                                      {
                                                                          UserId = userId,
                                                                          StartTimeUtc = startTimeUtc,
                                                                          EndTimeUtc =
                                                                                  item.EndTime.NormalizeToUtc(),
                                                                          StartTimeEastern =
                                                                                  _dateTimeProvider
                                                                                         .ConvertToEst(startTimeUtc),
                                                                          RoutingStatus = item.RoutingStatus
                                                                      };
                                                           }));
            }
        }

        return (primary, routing);
    }
}
