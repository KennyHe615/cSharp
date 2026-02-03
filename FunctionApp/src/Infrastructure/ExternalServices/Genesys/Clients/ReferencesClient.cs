using Application.Dtos.References;
using Application.References;
using Application.Shared.Context;
using Application.Shared.Enums;
using Application.Shared.Providers;

using Infrastructure.ExternalServices.FlurlHttp;

using Microsoft.Extensions.Logging;

using Shared.Extensions;


namespace Infrastructure.ExternalServices.Genesys.Clients;

public class ReferencesClient(IFlurlHttpClientFactory factory,
                              ILobContext lobContext,
                              ILogger<ReferencesClient> logger,
                              ITokenProvider tokenProvider)
    : GenesysApiClient(factory, lobContext, logger, tokenProvider), IReferencesClient
{
    private const int MaxPaginationIterations = 100;
    private const int PageSize = 100;
    private readonly string _queryParams = $"?pageSize={PageSize}";

    public async Task<List<GroupResponse>> GetGroupsAsync(CancellationToken cancellationToken)
    {
        const string category = nameof(SyncCategory.Groups);
        string url = $"/api/v2/{category}{_queryParams}";

        return await GetPaginatedAsync<GroupResponse>(url, category, cancellationToken);
    }

    public async Task<List<PresenceDefinitionResponse>> GetPresenceDefinitionsAsync(CancellationToken ct)
    {
        const string category = nameof(SyncCategory.PresenceDefinitions);
        string url = $"/api/v2/presence/definitions{_queryParams}";

        return await GetPaginatedAsync<PresenceDefinitionResponse>(url, category, ct);
    }

    public async Task<List<SkillResponse>> GetSkillsAsync(CancellationToken ct)
    {
        const string category = nameof(SyncCategory.Skills);
        string url = $"/api/v2/routing/{category}{_queryParams}";

        return await GetPaginatedAsync<SkillResponse>(url, category, ct);
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
