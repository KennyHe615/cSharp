using Application.References.Clients;
using Application.Shared.Context;
using Application.Shared.Providers;

using Configuration.Options;

using Infrastructure.ExternalServices.FlurlHttp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Extensions;
using Shared.Genesys.Models.References;


namespace Infrastructure.ExternalServices.Genesys.Clients;

public class ReferencesClient(IOptions<GenesysOptions> genesysOptions,
                              IFlurlHttpClientFactory factory,
                              ILobContext lobContext,
                              ILogger<ReferencesClient> logger,
                              ITokenProvider tokenProvider)
    : GenesysApiClient(genesysOptions, factory, lobContext, logger, tokenProvider), IReferencesClient
{
    private const int MaxPaginationIterations = 100;

    public async Task<List<GroupResponse>> GetGroupsAsync(CancellationToken cancellationToken)
    {
        return await GetPaginatedAsync<GroupResponse>("/api/v2/groups?pagesize=500", "Groups", cancellationToken);
    }

    public async Task<List<PresenceDefinitionResponse>> GetPresenceDefinitionsAsync(CancellationToken cancellationToken)
    {
        return await GetPaginatedAsync<PresenceDefinitionResponse>("/api/v2/presence/definitions?pageSize=500",
                                                                   "Presence Definitions",
                                                                   cancellationToken);
    }

    public async Task<List<SkillResponse>> GetSkillsAsync(CancellationToken cancellationToken)
    {
        return await GetPaginatedAsync<SkillResponse>("/api/v2/routing/skills?pageSize=500",
                                                      "Skills",
                                                      cancellationToken);
    }

    #region ========== *** Private Methods *** ==========

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
