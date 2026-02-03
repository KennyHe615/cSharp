using Application.Dtos.References;
using Application.References;
using Application.Shared.Context;
using Application.Shared.Enums;
using Application.Shared.Providers;

using Infrastructure.ExternalServices.FlurlHttp;

using Microsoft.Extensions.Logging;

using Shared.Extensions;


namespace Infrastructure.ExternalServices.Genesys.Clients;

/// <summary>
/// Provides access to PureCloud reference data endpoints (groups, skills, presence definitions, and wrapup codes).
/// </summary>
/// <remarks>
/// This client handles paginated retrieval of reference data from the PureCloud API, with built-in safeguards against infinite pagination loops.
/// All methods are LOB-scoped via <see cref="ILobContext"/>.
/// </remarks>
public class ReferencesClient(IFlurlHttpClientFactory factory,
                              ILobContext lobContext,
                              ILogger<ReferencesClient> logger,
                              ITokenProvider tokenProvider)
    : GenesysApiClient(factory, lobContext, logger, tokenProvider), IReferencesClient
{
    private const int MaxPaginationIterations = 100;
    private const int PageSize = 100;
    private readonly string _queryParams = $"?pageSize={PageSize}";

    /// <summary>
    /// Retrieves all groups from the PureCloud API using paginated requests.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>A list of all groups across all pages.</returns>
    /// <exception cref="ExternalServiceHttpException">Thrown when the API request fails or pagination limits are exceeded.</exception>
    public async Task<List<GroupResponse>> GetGroupsAsync(CancellationToken cancellationToken)
    {
        string url = $"/api/v2/groups{_queryParams}";

        return await GetPaginatedAsync<GroupResponse>(url, nameof(SyncCategory.Group), cancellationToken);
    }

    /// <summary>
    /// Retrieves all presence definitions from the PureCloud API using paginated requests.
    /// </summary>
    /// <param name="ct">Cancellation token to abort the operation.</param>
    /// <returns>A list of all presence definitions across all pages.</returns>
    /// <exception cref="ExternalServiceHttpException">Thrown when the API request fails or pagination limits are exceeded.</exception>
    public async Task<List<PresenceDefinitionResponse>> GetPresenceDefinitionsAsync(CancellationToken ct)
    {
        string url = $"/api/v2/presence/definitions{_queryParams}";

        return await GetPaginatedAsync<PresenceDefinitionResponse>(url, nameof(SyncCategory.PresenceDefinition), ct);
    }

    /// <summary>
    /// Retrieves all routing skills from the PureCloud API using paginated requests.
    /// </summary>
    /// <param name="ct">Cancellation token to abort the operation.</param>
    /// <returns>A list of all skills across all pages.</returns>
    /// <exception cref="ExternalServiceHttpException">Thrown when the API request fails or pagination limits are exceeded.</exception>
    public async Task<List<SkillResponse>> GetSkillsAsync(CancellationToken ct)
    {
        string url = $"/api/v2/routing/skills{_queryParams}";

        return await GetPaginatedAsync<SkillResponse>(url, nameof(SyncCategory.Skill), ct);
    }

    /// <summary>
    /// Retrieves all wrapup codes from the PureCloud API using paginated requests.
    /// </summary>
    /// <param name="ct">Cancellation token to abort the operation.</param>
    /// <returns>A list of all wrapup codes across all pages.</returns>
    /// <exception cref="ExternalServiceHttpException">Thrown when the API request fails or pagination limits are exceeded.</exception>
    public async Task<List<WrapupCodeResponse>> GetWrapupCodesAsync(CancellationToken ct)
    {
        string url = $"/api/v2/routing/wrapupcodes{_queryParams}";

        return await GetPaginatedAsync<WrapupCodeResponse>(url, nameof(SyncCategory.WrapupCode), ct);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Performs paginated retrieval of reference data from the PureCloud API.
    /// </summary>
    /// <typeparam name="T">The type of entity being retrieved.</typeparam>
    /// <param name="initialUrl">The initial API endpoint URL with query parameters.</param>
    /// <param name="entityName">The name of the entity type (used for logging and error messages).</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>A consolidated list of all entities retrieved across all pages.</returns>
    /// <exception cref="ExternalServiceHttpException">
    /// Thrown when:
    /// <list type="bullet">
    /// <item>The maximum pagination iteration limit is exceeded (prevents infinite loops).</item>
    /// <item>The API returns a 200 OK response with a missing or null entities array.</item>
    /// <item>Any HTTP-level error occurs during a request.</item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// This method follows the <c>nextUri</c> property in paginated responses until no further pages exist.
    /// A maximum iteration safeguard (<see cref="MaxPaginationIterations"/>) prevents runaway pagination.
    /// All errors are logged with structured context (LOB, entity name, retrieved count, pages processed).
    /// </remarks>
    private async Task<List<T>> GetPaginatedAsync<T>(string initialUrl,
                                                     string entityName,
                                                     CancellationToken cancellationToken = default)
    {
        List<T> results = [];
        string? currentUrl = initialUrl;
        int iterationCount = 0;

        try
        {
            while (!string.IsNullOrEmpty(currentUrl))
            {
                if (iterationCount >= MaxPaginationIterations)
                {
                    throw new ExternalServiceHttpException(System.Net.HttpStatusCode.InternalServerError,
                                                           "GET",
                                                           currentUrl,
                                                           $"[LOB: {LobContext.LobName}] Exceeded maximum pagination iterations ({MaxPaginationIterations}) for {entityName}");
                }

                PagedReferenceResponse<T>? response =
                    await GetAsync<PagedReferenceResponse<T>>(currentUrl, null, cancellationToken);

                if (response?.Entities == null)
                {
                    throw new ExternalServiceHttpException(System.Net.HttpStatusCode.OK,
                                                           "GET",
                                                           currentUrl,
                                                           $"[LOB: {LobContext.LobName}] Invalid response: Missing entities array for {entityName}",
                                                           null,
                                                           "API returned 200 OK but payload was missing the expected entities array.");
                }

                results.AddRange(response.Entities);
                currentUrl = response.NextUri;
                iterationCount++;
            }

            return results;
        }
        catch (Exception ex)
        {
            // The base client already logged the HTTP error.
            // We log the high-level pagination context here with full structured details.
            logger.LogError(ex,
                            "[LOB: {Lob}] Failed to complete pagination for {EntityName}. Retrieved {Count} entities across {Pages} pages before failure. {ExJson}",
                            LobContext.LobName,
                            entityName,
                            results.Count,
                            iterationCount,
                            ex.ToJson());

            throw;
        }
    }

    #endregion
}
