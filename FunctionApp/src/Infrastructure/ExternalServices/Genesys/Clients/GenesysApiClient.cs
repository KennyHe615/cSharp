using Application.Shared.Context;
using Application.Shared.Providers;

using Flurl.Http;

using Infrastructure.ExternalServices.FlurlHttp;

using Microsoft.Extensions.Logging;

using Shared.Constants;


namespace Infrastructure.ExternalServices.Genesys.Clients;

/// <summary>
/// Base client for Genesys API calls that:
/// <list type="bullet">
/// <item>Uses a shared <see cref="FlurlClient"/> from <see cref="IFlurlHttpClientFactory"/>.</item>
/// <item>Applies shared resiliency policies (retry + circuit breaker) via <see cref="FlurlHttpClient"/>.</item>
/// <item>Attaches an OAuth bearer token from <see cref="ITokenProvider"/> and triggers refresh on HTTP 401.</item>
/// </list>
/// </summary>
public abstract class GenesysApiClient : FlurlHttpClient
{
    private readonly ITokenProvider _tokenProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenesysApiClient"/> class.
    /// </summary>
    /// <param name="factory">Factory used to acquire and manage the underlying <see cref="FlurlClient"/>.</param>
    /// <param name="lobContext">Context of the current Line of Business, used for tenant-specific logging and isolation.</param>
    /// <param name="logger">Logger for capturing request/response lifecycle events.</param>
    /// <param name="tokenProvider">Provider responsible for providing and refreshing OAuth bearer tokens.</param>
    protected GenesysApiClient(IFlurlHttpClientFactory factory,
                               ILobContext lobContext,
                               ILogger<GenesysApiClient> logger,
                               ITokenProvider tokenProvider) : base(factory.GetOrAddClient(GenesysConstants.ApiBaseUrl),
                                                                    factory,
                                                                    lobContext,
                                                                    logger)
    {
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    /// <inheritdoc />
    public override async Task<T?> GetAsync<T>(string endpoint,
                                               Dictionary<string, string>? headers = null,
                                               CancellationToken cancellationToken = default) where T : default
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint must be provided.", nameof(endpoint));
        }

        // Use the PROTECTED fields inherited from FlurlHttpClient
        return await ExecuteWithPolicyAsync<T>(SafeMethodPolicy,
                                               endpoint,
                                               HttpMethod.Get,
                                               async (req, ct) =>
                                               {
                                                   using IFlurlResponse? resp = await req
                                                       .GetAsync(cancellationToken: ct)
                                                       .ConfigureAwait(false);

                                                   T data = await resp.GetJsonAsync<T>().ConfigureAwait(false);

                                                   return (data, resp.StatusCode);
                                               },
                                               headers,
                                               AddBearerTokenAsync,
                                               _tokenProvider.RefreshTokenAsync,
                                               cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint,
                                                                          TRequest payload,
                                                                          Dictionary<string, string>? headers = null,
                                                                          CancellationToken cancellationToken = default)
        where TResponse : default
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint must be provided.", nameof(endpoint));
        }

        return await ExecuteWithPolicyAsync(UnsafeMethodPolicy,
                                            endpoint,
                                            HttpMethod.Post,
                                            async (req, ct) =>
                                            {
                                                using IFlurlResponse? res = await req
                                                    .PostJsonAsync(payload, cancellationToken: ct)
                                                    .ConfigureAwait(false);

                                                TResponse data = await res.GetJsonAsync<TResponse>()
                                                                          .ConfigureAwait(false);

                                                return (data, res.StatusCode);
                                            },
                                            headers,
                                            AddBearerTokenAsync,
                                            _tokenProvider.RefreshTokenAsync,
                                            cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint,
                                                                         TRequest payload,
                                                                         Dictionary<string, string>? headers = null,
                                                                         CancellationToken cancellationToken = default)
        where TResponse : default
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint must be provided.", nameof(endpoint));
        }

        return await ExecuteWithPolicyAsync(UnsafeMethodPolicy,
                                            endpoint,
                                            HttpMethod.Put,
                                            async (req, ct) =>
                                            {
                                                using IFlurlResponse res = await req
                                                    .PutJsonAsync(payload, cancellationToken: ct)
                                                    .ConfigureAwait(false);

                                                TResponse data = await res.GetJsonAsync<TResponse>()
                                                                          .ConfigureAwait(false);

                                                return (data, res.StatusCode);
                                            },
                                            headers,
                                            AddBearerTokenAsync,
                                            _tokenProvider.RefreshTokenAsync,
                                            cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<bool> DeleteAsync(string endpoint,
                                                 Dictionary<string, string>? headers = null,
                                                 CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint must be provided.", nameof(endpoint));
        }

        return await ExecuteWithPolicyAsync(UnsafeMethodPolicy,
                                            endpoint,
                                            HttpMethod.Delete,
                                            async (req, ct) =>
                                            {
                                                using IFlurlResponse res = await req.DeleteAsync(cancellationToken: ct)
                                                    .ConfigureAwait(false);

                                                return (res.ResponseMessage.IsSuccessStatusCode, res.StatusCode);
                                            },
                                            headers,
                                            AddBearerTokenAsync,
                                            _tokenProvider.RefreshTokenAsync,
                                            cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Adds the current OAuth bearer token to the outgoing request.
    /// The underlying execution pipeline may invoke this multiple times across retries.
    /// </summary>
    private async Task AddBearerTokenAsync(IFlurlRequest req, CancellationToken ct)
    {
        string token = await _tokenProvider.GetValidTokenAsync(ct).ConfigureAwait(false);

        req.WithOAuthBearerToken(token);
    }
}
