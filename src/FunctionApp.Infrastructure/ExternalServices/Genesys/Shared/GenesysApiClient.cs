using FunctionApp.Application.Shared.Context;
using FunctionApp.Configuration.Options;
using FunctionApp.Infrastructure.ExternalServices.FlurlHttp;
using FunctionApp.Infrastructure.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.ExternalServices.Genesys.Shared;

/// <summary>
/// Specialized client for Genesys API calls.
/// Inherits all resilience (Retry, Circuit Breaker) and configuration from FlurlHttpClient.
/// </summary>
public class GenesysApiClient(IOptions<MultiLobOptions> multiLobOptions,
                              IFlurlHttpClientFactory factory,
                              ILobContext lobContext,
                              ILogger<GenesysApiClient> logger,
                              ITokenProvider tokenProvider) : FlurlHttpClient(
    factory.GetOrAddClient(multiLobOptions.Value.GenesysApiEndpoint),
    factory,
    lobContext,
    logger,
    async ct => await tokenProvider.GetValidTokenAsync(ct),
    async ct => await tokenProvider.RefreshTokenAsync(ct));
