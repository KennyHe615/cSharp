using FunctionApp.Application.References.Clients;
using FunctionApp.Application.References.DTOs;
using FunctionApp.Configuration.Options;
using FunctionApp.Infrastructure.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.ExternalServices.Genesys.Clients;

public class PresenceDefinitionClient(IOptions<GenesysOptions> genesysOptions,
                                      IOptions<FlurlClientOptions> flurlOptions,
                                      ILogger<PresenceDefinitionClient> logger,
                                      ITokenProvider tokenProvider)
    : GenesysReferencesClient(genesysOptions, flurlOptions, logger, tokenProvider), IPresenceDefinitionClient
{
    public async Task<List<PresenceDefinitionDto>> GetPresenceDefinitionsAsync(CancellationToken cancellationToken)
    {
        return await GetPaginatedAsync<PresenceDefinitionDto>("/api/v2/presence/definitions?pageSize=500",
                                                              "Presence Definitions",
                                                              cancellationToken);
    }
}
