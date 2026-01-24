using FunctionApp.Application.References.DTOs;
using FunctionApp.Configuration.Options;
using FunctionApp.Infrastructure.ExternalServices.Genesys.Shared;
using FunctionApp.Infrastructure.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.ExternalServices.Genesys.Clients;

/// <summary>
/// Base implementation for Genesys Reference-type clients.
/// Only accessible within the Infrastructure layer.
/// </summary>
public abstract class GenesysReferencesClient(IOptions<GenesysOptions> genesysOptions,
                                              IOptions<FlurlClientOptions> flurlOptions,
                                              ILogger logger,
                                              ITokenProvider tokenProvider)
    : GenesysApiClient(genesysOptions, flurlOptions, (ILogger<GenesysApiClient>)logger, tokenProvider)
{
    private const int MaxPaginationIterations = 100;

    protected async Task<List<T>> GetPaginatedAsync<T>(string initialUrl,
                                                       string entityName,
                                                       CancellationToken cancellationToken = default)
    {
        List<T> results = [];
        string? currentUrl = initialUrl;
        int iterationCount = 0;

        while (!string.IsNullOrEmpty(currentUrl))
        {
            if (iterationCount >= MaxPaginationIterations)
            {
                logger.LogError("Exceeded maximum pagination iterations ({Max}) for {EntityName}",
                                MaxPaginationIterations,
                                entityName);

                throw new InvalidOperationException($"Exceeded maximum pagination iterations for {entityName}");
            }

            PagedResponseDto<T>? response = await GetAsync<PagedResponseDto<T>>(currentUrl, null, cancellationToken);

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

        logger.LogInformation("Successfully fetched {Count} {EntityName} entities across {Pages} pages",
                              results.Count,
                              entityName,
                              iterationCount);

        return results;
    }
}
