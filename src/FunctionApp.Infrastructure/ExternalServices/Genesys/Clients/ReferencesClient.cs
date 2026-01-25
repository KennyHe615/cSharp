using FunctionApp.Application.References.Clients;
using FunctionApp.Application.References.DTOs;
using FunctionApp.Configuration.Options;
using FunctionApp.Infrastructure.ExternalServices.Genesys.Shared;
using FunctionApp.Infrastructure.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.ExternalServices.Genesys.Clients;

public class ReferencesClient(IOptions<GenesysOptions> genesysOptions,
                              IOptions<FlurlClientOptions> flurlOptions,
                              ILogger<ReferencesClient> logger,
                              ITokenProvider tokenProvider)
    : GenesysApiClient(genesysOptions, flurlOptions, logger, tokenProvider), IReferencesClient
{
    private const int MaxPaginationIterations = 100;

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
                    logger.LogError("Exceeded maximum pagination iterations ({Max}) for {EntityName}",
                                    MaxPaginationIterations,
                                    entityName);

                    throw new InvalidOperationException($"Exceeded maximum pagination iterations for {entityName}");
                }

                try
                {
                    PagedResponseDto<T>? response =
                        await GetAsync<PagedResponseDto<T>>(currentUrl, null, cancellationToken);

                    if (response?.Entities == null)
                    {
                        logger.LogError("Invalid response: Missing entities array for {EntityName} at {Url}",
                                        entityName,
                                        currentUrl);

                        throw new InvalidOperationException($"Invalid response for {entityName}");
                    }

                    results.AddRange(response.Entities);
                    currentUrl = response.NextUri;
                    iterationCount++;
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    logger.LogError(ex,
                                    "Error fetching page {Page} for {EntityName} at URL: {Url}",
                                    iterationCount + 1,
                                    entityName,
                                    currentUrl);

                    throw;
                }
            }

            logger.LogInformation("Successfully fetched {Count} {EntityName} entities across {Pages} pages",
                                  results.Count,
                                  entityName,
                                  iterationCount);

            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                            "Failed to complete pagination for {EntityName}. Retrieved {Count} entities across {Pages} pages before failure",
                            entityName,
                            results.Count,
                            iterationCount);

            throw;
        }
    }
}
