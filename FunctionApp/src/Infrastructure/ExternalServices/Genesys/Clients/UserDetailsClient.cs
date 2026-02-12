using Application.Common.Abstractions.Context;
using Application.Common.Abstractions.Providers;
using Application.Common.Factories;
using Application.Common.Models;
using Application.Contracts.UserDetails;
using Application.UserDetails;

using Infrastructure.ExternalServices.FlurlHttp;

using Microsoft.Extensions.Logging;

using Shared.Constants;
using Shared.Extensions;


namespace Infrastructure.ExternalServices.Genesys.Clients;

/// <summary>
/// Implementation of <see cref="IUserDetailsClient"/> for the Genesys Analytics API.
/// </summary>
public class UserDetailsClient(IFlurlHttpClientFactory factory,
                               ILobContext lobContext,
                               ILogger<UserDetailsClient> logger,
                               ITokenProvider tokenProvider)
    : GenesysApiClient(factory, lobContext, logger, tokenProvider), IUserDetailsClient
{
    private const string Url = "/api/v2/analytics/users/details/query";
    private const string DefaultQueryOrder = GenesysConstants.DefaultQueryOrder;
    private const int DefaultPageSize = GenesysConstants.DefaultPageSize;

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="pageNum"/> is less than 1 or when <paramref name="queryInterval"/> is not valid.
    /// </exception>
    /// <exception cref="ExternalServiceHttpException">
    /// Thrown when the API returns an error or null response.
    /// </exception>
    public async Task<UserDetailsResponse> GetUserDetailsAsync(string queryInterval,
                                                               int pageNum,
                                                               int? pageSize,
                                                               CancellationToken ct)
    {
        if (pageNum < 1)
        {
            throw new ArgumentException("Page number must be greater than or equal to 1.", nameof(pageNum));
        }

        UserDetailsResponse? response;

        try
        {
            Interval interval = IntervalFactory.FromString(queryInterval);

            UserDetailsRequest request = new()
                                         {
                                             Order = DefaultQueryOrder,
                                             Interval = interval.ToString(),
                                             Paging = new Paging
                                                      {
                                                          PageSize = pageSize ?? DefaultPageSize,
                                                          PageNumber = pageNum
                                                      }
                                         };

            response = await PostAsync<UserDetailsRequest, UserDetailsResponse>(Url, request, null, ct);
        }
        catch (Exception ex)
        {
            // The base client already logged the HTTP error.
            // We log the high-level pagination context here with full structured details.
            logger.LogErrorWithDetails(ex,
                                       CommonConstants.LobLogPrefix +
                                       "Failed to complete User Details synchronization.",
                                       LobContext.LobName);

            throw;
        }

        if (response != null) return response;

        string prefix = CommonConstants.LobLogPrefix.Replace("{LobName}", LobContext.LobName);

        throw new ExternalServiceHttpException(System.Net.HttpStatusCode.OK,
                                               "POST",
                                               Url,
                                               $"{prefix}Invalid response: null response from API.",
                                               null,
                                               "API returned 200 OK but null response body.");
    }
}
