using System.Net;
using System.Security.Cryptography;
using System.Text;

using Application.Abstractions.Context;

using Flurl.Http;

using Infrastructure.ExternalApis.Abstractions;
using Infrastructure.ExternalApis.Shared.Policies;

using Microsoft.Extensions.Logging;

using Polly;

using SharedKernel.Logging;


namespace Infrastructure.ExternalApis.Shared.Http;

/// <summary>
/// Flurl-based implementation of <see cref="IHttpApiClient"/> that executes outbound calls
/// through shared Polly policies and emits LOB-scoped structured logs.
/// </summary>
public class HttpApiClient : IHttpApiClient
{
    #region ========== *** Properties and Constructor *** ==========

    private const string LogCategory = "ExternalApi.Http";

    private readonly FlurlClient _client;
    private readonly ILobContext _lobContext;
    private readonly ILogger<HttpApiClient> _logger;
    private readonly IAsyncPolicy _safePolicy;
    private readonly IAsyncPolicy _unsafePolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpApiClient"/> class.
    /// </summary>
    /// <param name="client">Underlying Flurl client.</param>
    /// <param name="factory">HTTP client factory that provides policies.</param>
    /// <param name="lobContext">Current LOB context for logging and policy context.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any constructor dependency is null.
    /// </exception>
    public HttpApiClient(FlurlClient client,
                         IHttpApiClientFactory factory,
                         ILobContext lobContext,
                         ILogger<HttpApiClient> logger)
    {
        _client = client         ?? throw new ArgumentNullException(nameof(client));
        _lobContext = lobContext ?? throw new ArgumentNullException(nameof(lobContext));
        _logger = logger         ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(factory);

        _safePolicy = factory.GetSafePolicy();
        _unsafePolicy = factory.GetUnsafePolicy();
    }

    #endregion

    public string BaseUrl => _client.BaseUrl;

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the outbound HTTP request fails.</exception>
    public Task<T?> GetAsync<T>(string endpoint,
                                Dictionary<string, string>? headers = null,
                                CancellationToken ct = default)
    {
        return ExecuteWithPolicyAsync(_safePolicy,
                                      endpoint,
                                      HttpMethod.Get,
                                      async (req, token) =>
                                      {
                                          using IFlurlResponse res = await req.GetAsync(cancellationToken: token)
                                                                              .ConfigureAwait(false);
                                          T data = await res.GetJsonAsync<T>()
                                                            .ConfigureAwait(false);

                                          return (data, res.StatusCode);
                                      },
                                      headers,
                                      cancellationToken: ct);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the outbound HTTP request fails.</exception>
    public Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint,
                                                           TRequest payload,
                                                           Dictionary<string, string>? headers = null,
                                                           CancellationToken ct = default)
    {
        return ExecuteWithPolicyAsync(_unsafePolicy,
                                      endpoint,
                                      HttpMethod.Post,
                                      async (req, token) =>
                                      {
                                          using IFlurlResponse res =
                                              await req.PostJsonAsync(payload, cancellationToken: token)
                                                       .ConfigureAwait(false);
                                          TResponse data = await res.GetJsonAsync<TResponse>()
                                                                    .ConfigureAwait(false);

                                          return (data, res.StatusCode);
                                      },
                                      headers,
                                      cancellationToken: ct);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the outbound HTTP request fails.</exception>
    public Task<TResponse?> PostUrlEncodedAsync<TResponse>(string endpoint,
                                                           object payload,
                                                           Dictionary<string, string>? headers = null,
                                                           CancellationToken ct = default)
    {
        return ExecuteWithPolicyAsync(_unsafePolicy,
                                      endpoint,
                                      HttpMethod.Post,
                                      async (req, token) =>
                                      {
                                          using IFlurlResponse res =
                                              await req.PostUrlEncodedAsync(payload, cancellationToken: token)
                                                       .ConfigureAwait(false);
                                          TResponse data = await res.GetJsonAsync<TResponse>()
                                                                    .ConfigureAwait(false);

                                          return (data, res.StatusCode);
                                      },
                                      headers,
                                      cancellationToken: ct);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the outbound HTTP request fails.</exception>
    public Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint,
                                                          TRequest payload,
                                                          Dictionary<string, string>? headers = null,
                                                          CancellationToken ct = default)
    {
        return ExecuteWithPolicyAsync(_unsafePolicy,
                                      endpoint,
                                      HttpMethod.Put,
                                      async (req, token) =>
                                      {
                                          using IFlurlResponse res =
                                              await req.PutJsonAsync(payload, cancellationToken: token)
                                                       .ConfigureAwait(false);
                                          TResponse data = await res.GetJsonAsync<TResponse>()
                                                                    .ConfigureAwait(false);

                                          return (data, res.StatusCode);
                                      },
                                      headers,
                                      cancellationToken: ct);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the outbound HTTP request fails.</exception>
    public Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint,
                                                            TRequest payload,
                                                            Dictionary<string, string>? headers = null,
                                                            CancellationToken ct = default)
    {
        return ExecuteWithPolicyAsync(_unsafePolicy,
                                      endpoint,
                                      HttpMethod.Patch,
                                      async (req, token) =>
                                      {
                                          using IFlurlResponse res =
                                              await req.PatchJsonAsync(payload, cancellationToken: token)
                                                       .ConfigureAwait(false);
                                          TResponse data = await res.GetJsonAsync<TResponse>()
                                                                    .ConfigureAwait(false);

                                          return (data, res.StatusCode);
                                      },
                                      headers,
                                      cancellationToken: ct);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the outbound HTTP request fails.</exception>
    public Task DeleteAsync(string endpoint, Dictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        return ExecuteWithPolicyAsync<object?>(_unsafePolicy,
                                               endpoint,
                                               HttpMethod.Delete,
                                               async (req, token) =>
                                               {
                                                   using IFlurlResponse res =
                                                       await req.DeleteAsync(cancellationToken: token)
                                                                .ConfigureAwait(false);

                                                   return (null, res.StatusCode);
                                               },
                                               headers,
                                               cancellationToken: ct);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the outbound HTTP request fails.</exception>
    public Task HeadAsync(string endpoint, Dictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        return ExecuteWithPolicyAsync<object?>(_safePolicy,
                                               endpoint,
                                               HttpMethod.Head,
                                               async (req, token) =>
                                               {
                                                   using IFlurlResponse res =
                                                       await req.HeadAsync(cancellationToken: token)
                                                                .ConfigureAwait(false);

                                                   return (null, res.StatusCode);
                                               },
                                               headers,
                                               cancellationToken: ct);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Executes an HTTP operation through the specified policy pipeline.
    /// </summary>
    /// <typeparam name="T">Result payload type.</typeparam>
    /// <param name="policy">Policy used to execute the operation.</param>
    /// <param name="endpoint">Relative endpoint path.</param>
    /// <param name="method">HTTP method metadata for logs/errors.</param>
    /// <param name="operation">Operation delegate that executes the request.</param>
    /// <param name="headers">Optional request headers.</param>
    /// <param name="configurator">Optional request configurator (e.g., auth).</param>
    /// <param name="onUnauthorized">Optional 401 refresh callback passed via Polly context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation result payload.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required arguments are null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the outbound HTTP request fails.</exception>
    private async Task<T?> ExecuteWithPolicyAsync<T>(IAsyncPolicy policy,
                                                     string endpoint,
                                                     HttpMethod method,
                                                     Func<IFlurlRequest, CancellationToken,
                                                         Task<(T? Result, int StatusCode)>> operation,
                                                     Dictionary<string, string>? headers = null,
                                                     Func<IFlurlRequest, CancellationToken, Task>? configurator = null,
                                                     Func<CancellationToken, Task>? onUnauthorized = null,
                                                     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(operation);

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint must be provided.", nameof(endpoint));
        }

        string fullUrl = CombineUrl(_client.BaseUrl, endpoint);

        Polly.Context context = CreatePolicyContext(onUnauthorized);

        using IDisposable _ = _logger.BeginOperationScope(_lobContext.LobName, LogCategory, method.Method);

        return await policy.ExecuteAsync<T?>((_, ct) => ExecuteRequestCoreAsync(endpoint,
                                                                                method,
                                                                                fullUrl,
                                                                                operation,
                                                                                headers,
                                                                                configurator,
                                                                                ct),
                                             context,
                                             cancellationToken)
                           .ConfigureAwait(false);
    }

    private Polly.Context CreatePolicyContext(Func<CancellationToken, Task>? onUnauthorized)
    {
        return new Polly.Context
               {
                   [HttpPolicyContextKeys.Lob] = _lobContext.LobName.Value,
                   [HttpPolicyContextKeys.RefreshFunc] = onUnauthorized
               };
    }

    private async Task<T?> ExecuteRequestCoreAsync<T>(string endpoint,
                                                      HttpMethod method,
                                                      string fullUrl,
                                                      Func<IFlurlRequest, CancellationToken,
                                                          Task<(T? Result, int StatusCode)>> operation,
                                                      Dictionary<string, string>? headers,
                                                      Func<IFlurlRequest, CancellationToken, Task>? configurator,
                                                      CancellationToken ct)
    {
        try
        {
            LogRequest(method.Method, fullUrl);

            IFlurlRequest request = _client.Request(endpoint);

            ApplyHeaders(request, headers);

            if (configurator is not null)
            {
                await configurator(request, ct)
                   .ConfigureAwait(false);
            }

            (T? result, int statusCode) = await operation(request, ct)
               .ConfigureAwait(false);

            LogResponse(method.Method, fullUrl, statusCode);

            return result;
        }
        catch (FlurlHttpException ex)
        {
            string? responseSummary = await TryGetSafeResponseSummaryAsync(ex)
               .ConfigureAwait(false);
            HttpStatusCode? statusCode = ex.StatusCode is {} sc ? (HttpStatusCode)sc : null;

            ExternalServiceHttpException wrapped = BuildWrappedHttpException(ex,
                                                                             method.Method,
                                                                             fullUrl,
                                                                             responseSummary,
                                                                             statusCode);

            _logger.LogErrorWithDetails(wrapped,
                                        LobLogTemplates.LobCategory
                                        + "HTTP failure | Method: {Method} | Url: {Url} | Status: {Status} | Response: {ResponseSummary}",
                                        _lobContext.LobName.Value,
                                        LogCategory,
                                        method.Method,
                                        fullUrl,
                                        statusCode?.ToString() ?? "none",
                                        responseSummary        ?? "none");

            throw wrapped;
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithDetails(ex,
                                        LobLogTemplates.LobCategory
                                        + "Unexpected HTTP pipeline failure | Method: {Method} | Url: {Url}",
                                        _lobContext.LobName.Value,
                                        LogCategory,
                                        method.Method,
                                        fullUrl);

            throw;
        }
    }

    private void LogRequest(string method, string fullUrl)
    {
        _logger.LogDebug(LobLogTemplates.LobCategory + "HTTP Request | {Method} {Url}",
                         _lobContext.LobName.Value,
                         LogCategory,
                         method,
                         fullUrl);
    }

    private void LogResponse(string method, string fullUrl, int statusCode)
    {
        _logger.LogDebug(LobLogTemplates.LobCategory + "HTTP Response | {Method} {Url} | Status: {StatusCode}",
                         _lobContext.LobName.Value,
                         LogCategory,
                         method,
                         fullUrl,
                         statusCode);
    }

    private static ExternalServiceHttpException BuildWrappedHttpException(
        FlurlHttpException ex,
        string method,
        string fullUrl,
        string? responseSummary,
        HttpStatusCode? statusCode)
    {
        return new ExternalServiceHttpException(statusCode,
                                                method,
                                                fullUrl,
                                                $"External API request failed: {method} {fullUrl}",
                                                ex,
                                                responseSummary,
                                                "HttpApiClient");
    }

    private static void ApplyHeaders(IFlurlRequest request, Dictionary<string, string>? headers)
    {
        if (headers is null || headers.Count == 0) return;

        foreach (KeyValuePair<string, string> kv in headers)
        {
            request.WithHeader(kv.Key, kv.Value);
        }
    }

    private static string CombineUrl(string baseUrl, string endpoint)
    {
        return string.IsNullOrWhiteSpace(baseUrl) ? endpoint : $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
    }

    private static async Task<string?> TryGetSafeResponseSummaryAsync(FlurlHttpException ex)
    {
        try
        {
            string body = await ex.GetResponseStringAsync()
                                  .ConfigureAwait(false);

            if (string.IsNullOrEmpty(body)) return null;

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(body));
            string digest = Convert.ToHexString(hash[..8]);

            return $"len={body.Length},sha256_8={digest}";
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
