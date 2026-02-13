using Application.Common.Abstractions.Context;
using Application.Common.Enums;
using Application.Common.Models;
using Application.Contracts.UserDetails;
using Application.Dtos.UserDetails;
using Application.Normalizers.UserDetails;
using Application.UserDetails;

using Microsoft.Extensions.Logging;

using Shared.Constants;


namespace Infrastructure.Services.UserDetails;

public abstract class UserDetailsSyncServiceBase(IUserDetailsClient client,
                                                 IUserDetailsNormalizer normalizer,
                                                 IUserDetailsRepository repository,
                                                 ILobContext lobContext,
                                                 ILogger logger)
{
    private readonly IUserDetailsClient _client = client ?? throw new ArgumentNullException(nameof(client));

    private readonly IUserDetailsRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    protected readonly ILobContext LobContext = lobContext ?? throw new ArgumentNullException(nameof(lobContext));
    protected readonly ILogger Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected async Task SyncIntervalPageAsync(Interval interval, int pageNumber, CancellationToken ct)
    {
        string lobName = LobContext.LobName;
        const string category = nameof(SyncCategory.UserDetailsIncremental);
        UserDetailsResponse response;

        try
        {
            response = await _client.GetUserDetailsAsync(interval.ToString(), pageNumber, null, ct)
                                    .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                            CommonConstants.LobCategoryLogPrefix +
                            "Failed to fetch user details for interval {Interval}, page {Page}",
                            lobName,
                            category,
                            interval,
                            pageNumber);

            throw;
        }

        int totalHits = response.TotalHits;

        if (totalHits == 0)
        {
            Logger.LogWarning(
                CommonConstants.LobCategoryLogPrefix + "No data found for interval {Interval}, page {Page}",
                lobName,
                nameof(SyncCategory.UserDetailsIncremental),
                interval,
                pageNumber);

            return;
        }

        Logger.LogDebug(
            CommonConstants.LobCategoryLogPrefix + "Fetched {Count} records for interval {Interval}, page {Page}",
            lobName,
            category,
            response.UserDetails.Count,
            interval,
            pageNumber);

        // Normalize here instead of in repository
        (List<PrimaryPresenceDto> primaryDtos, List<RoutingStatusDto> routingDtos) =
            normalizer.Normalize(response.UserDetails);

        // Pass DTOs to repository
        await _repository.UpsertUserDetailsAsync(primaryDtos, routingDtos, ct);
    }
}
