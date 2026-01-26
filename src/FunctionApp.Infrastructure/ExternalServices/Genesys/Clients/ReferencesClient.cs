using FunctionApp.Application.References.Clients;
using FunctionApp.Application.References.DTOs;
using FunctionApp.Application.Shared.Context;
using FunctionApp.Configuration.Options;
using FunctionApp.Infrastructure.ExternalServices.FlurlHttp;
using FunctionApp.Infrastructure.ExternalServices.Genesys.Shared;
using FunctionApp.Infrastructure.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.ExternalServices.Genesys.Clients;

public class ReferencesClient(IOptions<MultiLobOptions> multiLobOptions,
                              IFlurlHttpClientFactory factory,
                              ILobContext lobContext,
                              ILogger<ReferencesClient> logger,
                              ITokenProvider tokenProvider)
    : GenesysApiClient(multiLobOptions, factory, lobContext, logger, tokenProvider), IReferencesClient
{
    private const int MaxPaginationIterations = 100;

    private string? LobName => LobContext.LobName;

    public async Task<List<GroupResponseDto>> GetGroupsAsync(CancellationToken cancellationToken)
    {
        return await GetPaginatedAsync<GroupResponseDto>("/api/v2/groups?pagesize=500", "Groups", cancellationToken);
    }

    public async Task<List<PresenceDefinitionResponseDto>> GetPresenceDefinitionsAsync(
        CancellationToken cancellationToken)
    {
        return await GetPaginatedAsync<PresenceDefinitionResponseDto>("/api/v2/presence/definitions?pageSize=500",
                                                                      "Presence Definitions",
                                                                      cancellationToken);
    }

    public async Task<List<SkillResponseDto>> GetSkillsAsync(CancellationToken cancellationToken)
    {
        return await GetPaginatedAsync<SkillResponseDto>("/api/v2/routing/skills?pageSize=500",
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
                    logger.LogError("[LOB: {Lob}] Exceeded maximum pagination iterations ({Max}) for {EntityName}",
                                    LobName,
                                    MaxPaginationIterations,
                                    entityName);

                    throw new InvalidOperationException(
                        $"[LOB: {LobName}] Exceeded maximum pagination iterations for {entityName}");
                }

                try
                {
                    PagedResponseDto<T>? response =
                        await GetAsync<PagedResponseDto<T>>(currentUrl, null, cancellationToken);

                    if (response?.Entities == null)
                    {
                        logger.LogError(
                            "[LOB: {Lob}] Invalid response: Missing entities array for {EntityName} at {Url}",
                            LobName,
                            entityName,
                            currentUrl);

                        throw new InvalidOperationException($"[LOB: {LobName}] Invalid response for {entityName}");
                    }

                    results.AddRange(response.Entities);
                    currentUrl = response.NextUri;
                    iterationCount++;
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    logger.LogError(ex,
                                    "[LOB: {Lob}] Error fetching page {Page} for {EntityName} at URL: {Url}",
                                    LobName,
                                    iterationCount + 1,
                                    entityName,
                                    currentUrl);

                    throw;
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                            "[LOB: {Lob}] Failed to complete pagination for {EntityName}. Retrieved {Count} entities across {Pages} pages before failure",
                            LobName,
                            entityName,
                            results.Count,
                            iterationCount);

            throw;
        }
    }

    #endregion
}
