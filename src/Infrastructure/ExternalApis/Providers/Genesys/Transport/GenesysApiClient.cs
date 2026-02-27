using System.Net;

using Application.Abstractions.Context;

using Infrastructure.ExternalApis.Abstractions;
using Infrastructure.ExternalApis.Providers.Genesys.Auth.Abstractions;
using Infrastructure.ExternalApis.Providers.Genesys.Configuration;
using Infrastructure.ExternalApis.Shared.Http;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Infrastructure.ExternalApis.Providers.Genesys.Transport;

/// <summary>
/// Genesys-aware HTTP client that composes generic HTTP transport with OAuth token handling.
/// </summary>
public sealed class GenesysApiClient : IGenesysApiClient
{
    private readonly HttpApiClient _httpApiClient;
    private readonly IGenesysTokenProvider _tokenProvider;

    public GenesysApiClient(ILobContext lobContext,
                            IHttpApiClientFactory httpApiClientFactory,
                            IOptions<GenesysOptions> options,
                            ILogger<HttpApiClient> httpApiClientLogger,
                            IGenesysTokenProvider tokenProvider)
    {
        ArgumentNullException.ThrowIfNull(lobContext);
        ArgumentNullException.ThrowIfNull(httpApiClientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpApiClientLogger);

        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        GenesysOptions optionValue =
            options.Value ?? throw new InvalidOperationException("GenesysOptions is not configured.");
        _httpApiClient = new HttpApiClient(httpApiClientFactory.GetOrAddClient(optionValue.ApiBaseUrl),
                                           httpApiClientFactory,
                                           lobContext,
                                           httpApiClientLogger);
    }

    /// <inheritdoc />
    public string BaseUrl => _httpApiClient.BaseUrl;

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string endpoint,
                                Dictionary<string, string>? headers = null,
                                CancellationToken ct = default)
    {
        return ExecuteWithAuthRetryAsync((authHeaders, token) =>
                                             _httpApiClient.GetAsync<T>(endpoint, authHeaders, token),
                                         headers,
                                         ct);
    }

    /// <inheritdoc />
    public Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint,
                                                           TRequest payload,
                                                           Dictionary<string, string>? headers = null,
                                                           CancellationToken ct = default)
    {
        return ExecuteWithAuthRetryAsync((authHeaders, token) =>
                                             _httpApiClient.PostAsync<TRequest, TResponse>(endpoint,
                                              payload,
                                              authHeaders,
                                              token),
                                         headers,
                                         ct);
    }

    /// <inheritdoc />
    public Task<TResponse?> PostUrlEncodedAsync<TResponse>(string endpoint,
                                                           object payload,
                                                           Dictionary<string, string>? headers = null,
                                                           CancellationToken ct = default)
    {
        return ExecuteWithAuthRetryAsync((authHeaders, token) =>
                                             _httpApiClient.PostUrlEncodedAsync<TResponse>(endpoint,
                                              payload,
                                              authHeaders,
                                              token),
                                         headers,
                                         ct);
    }

    /// <inheritdoc />
    public Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint,
                                                          TRequest payload,
                                                          Dictionary<string, string>? headers = null,
                                                          CancellationToken ct = default)
    {
        return ExecuteWithAuthRetryAsync((authHeaders, token) =>
                                             _httpApiClient.PutAsync<TRequest, TResponse>(endpoint,
                                              payload,
                                              authHeaders,
                                              token),
                                         headers,
                                         ct);
    }

    /// <inheritdoc />
    public Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint,
                                                            TRequest payload,
                                                            Dictionary<string, string>? headers = null,
                                                            CancellationToken ct = default)
    {
        return ExecuteWithAuthRetryAsync((authHeaders, token) =>
                                             _httpApiClient.PatchAsync<TRequest, TResponse>(endpoint,
                                              payload,
                                              authHeaders,
                                              token),
                                         headers,
                                         ct);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string endpoint, Dictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        return ExecuteWithAuthRetryNoResultAsync((authHeaders, token) =>
                                                     _httpApiClient.DeleteAsync(endpoint, authHeaders, token),
                                                 headers,
                                                 ct);
    }

    /// <inheritdoc />
    public Task HeadAsync(string endpoint, Dictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        return ExecuteWithAuthRetryNoResultAsync((authHeaders, token) =>
                                                     _httpApiClient.HeadAsync(endpoint, authHeaders, token),
                                                 headers,
                                                 ct);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Executes a typed operation with bearer auth and retries once after token refresh on HTTP 401.
    /// </summary>
    private async Task<T?> ExecuteWithAuthRetryAsync<T>(
        Func<Dictionary<string, string>, CancellationToken, Task<T?>> operation,
        Dictionary<string, string>? headers,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(operation);

        string token = await _tokenProvider.GetValidTokenAsync(ct)
                                           .ConfigureAwait(false);

        try
        {
            return await operation(BuildHeaders(headers, token), ct)
               .ConfigureAwait(false);
        }
        catch (ExternalServiceHttpException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _tokenProvider.RefreshTokenAsync(ct)
                                .ConfigureAwait(false);
            string refreshedToken = await _tokenProvider.GetValidTokenAsync(ct)
                                                        .ConfigureAwait(false);

            return await operation(BuildHeaders(headers, refreshedToken), ct)
               .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes a non-returning operation with bearer auth and retries once after token refresh on HTTP 401.
    /// </summary>
    private async Task ExecuteWithAuthRetryNoResultAsync(
        Func<Dictionary<string, string>, CancellationToken, Task> operation,
        Dictionary<string, string>? headers,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(operation);

        string token = await _tokenProvider.GetValidTokenAsync(ct)
                                           .ConfigureAwait(false);

        try
        {
            await operation(BuildHeaders(headers, token), ct)
               .ConfigureAwait(false);
        }
        catch (ExternalServiceHttpException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _tokenProvider.RefreshTokenAsync(ct)
                                .ConfigureAwait(false);
            string refreshedToken = await _tokenProvider.GetValidTokenAsync(ct)
                                                        .ConfigureAwait(false);

            await operation(BuildHeaders(headers, refreshedToken), ct)
               .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates request headers merged with the OAuth bearer token.
    /// </summary>
    private static Dictionary<string, string> BuildHeaders(Dictionary<string, string>? headers, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Genesys OAuth token cannot be null or empty.");
        }

        Dictionary<string, string> merged = headers is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);

        merged["Authorization"] = $"Bearer {token}";

        return merged;
    }

    #endregion
}
