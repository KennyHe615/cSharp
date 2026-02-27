using System.Collections.Concurrent;
using System.Text.Json;

using Flurl.Http;
using Flurl.Http.Configuration;

using Infrastructure.ExternalApis.Abstractions;

using Microsoft.Extensions.Options;

using Polly;

using SharedKernel.Serialization.Json;


namespace Infrastructure.ExternalApis.Shared.Http;

/// <summary>
/// Default implementation of <see cref="IHttpApiClientFactory"/>.
/// </summary>
/// <remarks>
/// This factory centralizes:
/// <list type="bullet">
/// <item><description><see cref="FlurlClient"/> caching by base URL,</description></item>
/// <item><description>shared serializer/timeout configuration,</description></item>
/// <item><description>safe and unsafe resiliency policy reuse.</description></item>
/// </list>
/// </remarks>
public sealed class HttpApiClientFactory : IHttpApiClientFactory
{
    #region ========== *** Properties and Constructor *** ==========

    private readonly HttpClientResilienceOptions _options;

    private readonly ConcurrentDictionary<string, FlurlClient> _clients =
        new ConcurrentDictionary<string, FlurlClient>();

    private readonly IAsyncPolicy _safePolicy;
    private readonly IAsyncPolicy _unsafePolicy;
    private readonly Lazy<JsonSerializerOptions> _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpApiClientFactory"/> class.
    /// </summary>
    /// <param name="options">HTTP resilience and client configuration options.</param>
    /// <param name="policyFactory">Factory used to build shared resiliency policy pipelines.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="policyFactory"/> is null.
    /// </exception>
    public HttpApiClientFactory(IOptions<HttpClientResilienceOptions> options,
                                IHttpResiliencePolicyFactory policyFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(policyFactory);

        _options = options.Value ?? throw new ArgumentNullException(nameof(options));

        _jsonOptions = new Lazy<JsonSerializerOptions>(() =>
                                                       {
                                                           JsonSerializerOptions json = new JsonSerializerOptions
                                                               {
                                                                   PropertyNamingPolicy =
                                                                       JsonNamingPolicy.CamelCase,
                                                                   PropertyNameCaseInsensitive = true
                                                               };
                                                           json.AddSnakeUpperEnums();

                                                           return json;
                                                       });

        _safePolicy = policyFactory.CreateSafePolicy();
        _unsafePolicy = policyFactory.CreateUnsafePolicy();
    }

    #endregion

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="baseUrl"/> is null, empty, or whitespace.
    /// </exception>
    public FlurlClient GetOrAddClient(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Base URL must be provided.", nameof(baseUrl));
        }

        return _clients.GetOrAdd(baseUrl,
                                 url =>
                                 {
                                     FlurlClient client = new FlurlClient(url);
                                     client.Settings.JsonSerializer = new DefaultJsonSerializer(_jsonOptions.Value);
                                     client.Settings.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

                                     return client;
                                 });
    }

    /// <inheritdoc />
    public IAsyncPolicy GetSafePolicy()
    {
        return _safePolicy;
    }

    /// <inheritdoc />
    public IAsyncPolicy GetUnsafePolicy()
    {
        return _unsafePolicy;
    }

    /// <summary>
    /// Disposes all cached <see cref="FlurlClient"/> instances and clears the cache.
    /// </summary>
    public void Dispose()
    {
        foreach (FlurlClient client in _clients.Values)
        {
            client.Dispose();
        }

        _clients.Clear();
    }
}
