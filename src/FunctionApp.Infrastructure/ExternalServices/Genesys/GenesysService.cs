using FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

using Microsoft.Extensions.Logging;


namespace FunctionApp.Infrastructure.ExternalServices.Genesys;

public sealed class GenesysService(IHttpClient httpClient, ILogger<GenesysService> logger) : IGenesysService
{
    public async Task<T?> GetRoutingSkillsAsync<T>(int pageSize = 500, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching routing skills with pageSize={PageSize}", pageSize);

        T? response = await httpClient.GetAsync<T>(
            endpoint: $"/api/v2/routing/skills?pageSize={pageSize}",
            cancellationToken);

        if (response != null)
        {
            logger.LogInformation("Successfully fetched {Count} routing skills out of {Total} total", 999, 999);
        }

        return response;
    }
}
