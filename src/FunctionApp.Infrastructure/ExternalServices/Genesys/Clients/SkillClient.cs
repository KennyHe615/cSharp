using FunctionApp.Application.References.Clients;
using FunctionApp.Application.References.DTOs;
using FunctionApp.Configuration.Options;
using FunctionApp.Infrastructure.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.ExternalServices.Genesys.Clients;

public class SkillClient(IOptions<GenesysOptions> genesysOptions,
                         IOptions<FlurlClientOptions> flurlOptions,
                         ILogger<SkillClient> logger,
                         ITokenProvider tokenProvider)
    : GenesysReferencesClient(genesysOptions, flurlOptions, logger, tokenProvider), ISkillClient
{
    public async Task<List<SkillDto>> GetSkillsAsync(CancellationToken cancellationToken)
    {
        return await GetPaginatedAsync<SkillDto>("/api/v2/routing/skills?pageSize=500", "Skills", cancellationToken);
    }
}
