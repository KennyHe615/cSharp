using System.Net;

using Application.Shared.Context;

using Flurl.Http;

using Microsoft.Extensions.Logging;

using Polly;

using Shared.Extensions;


namespace Infrastructure.ExternalServices.FlurlHttp;

/// <summary>
/// HTTP client wrapper built on Flurl that executes requests through Polly policies,
/// adds optional headers/configuration, and normalizes failures into <see cref="ExternalServiceHttpException"/>.
/// </summary>
public class FlurlHttpClient : IFlurlHttpClient
{
    #region ========== *** Fields & Constructor *** ==========

    private readonly FlurlClient _client;
    private readonly ILogger _logger;

    /// <summary>
    /// Gets the current LOB context information used for logging and policy context.
    /// </summary>
    protected readonly ILobContext LobContext;

    /// <summary>
    /// Gets the Polly policy used for safe/idempotent HTTP methods (e.g., GET/HEAD).
    /// </summary>
    protected readonly IAsyncPolicy SafeMethodPolicy;

    /// <summary>
    /// Gets the Polly policy used for unsafe/non-idempotent HTTP methods (e.g., POST/PUT/PATCH/DELETE).
    /// </summary>
    protected readonly IAsyncPolicy UnsafeMethodPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlurlHttpClient"/> class.
    /// </summary>
    /// <param name="client">The underlying Flurl client used to build and execute requests.</param>
    /// <param name="factory">Factory providing Polly policies for safe/unsafe HTTP methods.</param>
    /// <param name="lobContext">LOB context used for policy context and logging.</param>
    /// <param name="logger">Logger used for request/response diagnostics and error reporting.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="client"/>, <paramref name="factory"/>, <paramref name="lobContext"/>, or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public FlurlHttpClient(FlurlClient client, IFlurlHttpClientFactory factory, ILobContext lobContext, ILogger logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(factory);

        LobContext = lobContext ?? throw new ArgumentNullException(nameof(lobContext));
        SafeMethodPolicy = factory.GetSafePolicy();
        UnsafeMethodPolicy = factory.GetUnsafePolicy();
        // Shared global policies
        // _client.OnError(HandleError); // Error handling is done in ExecuteWithPolicyAsync
    }

    public string BaseUrl => _client.BaseUrl;

    #endregion

    /// <summary>
    /// Sends a GET request to the specified endpoint and deserializes the response body as JSON.
    /// </summary>
    /// <typeparam name="T">The JSON response type.</typeparam>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns>The deserialized response, or <c>null</c> if the operation returns <c>null</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the external service request fails.</exception>
    public virtual async Task<T?> GetAsync<T>(string endpoint,
                                              Dictionary<string, string>? headers = null,
                                              CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(SafeMethodPolicy,
                                            endpoint,
                                            HttpMethod.Get,
                                            async (req, ct) =>
                                            {
                                                using IFlurlResponse res = await req.GetAsync(cancellationToken: ct)
                                                    .ConfigureAwait(false);

                                                T data = await res.GetJsonAsync<T>().ConfigureAwait(false);

                                                return (data, res.StatusCode);
                                            },
                                            headers,
                                            null,
                                            null,
                                            cancellationToken);
    }

    /// <summary>
    /// Sends a POST request with a JSON payload and deserializes the response body as JSON.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The JSON response type.</typeparam>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="payload">Payload serialized to JSON request body.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns>The deserialized response, or <c>null</c> if the operation returns <c>null</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the external service request fails.</exception>
    public virtual async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint,
                                                                         TRequest payload,
                                                                         Dictionary<string, string>? headers = null,
                                                                         CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(UnsafeMethodPolicy,
                                            endpoint,
                                            HttpMethod.Post,
                                            async (req, ct) =>
                                            {
                                                using IFlurlResponse res = await req
                                                    .PostJsonAsync(payload, cancellationToken: ct)
                                                    .ConfigureAwait(false);

                                                TResponse data = await res.GetJsonAsync<TResponse>()
                                                                          .ConfigureAwait(false);

                                                return (data, res.StatusCode);
                                            },
                                            headers,
                                            null,
                                            null,
                                            cancellationToken);
    }

    /// <summary>
    /// Sends a POST request with a URL\-encoded payload and deserializes the response body as JSON.
    /// </summary>
    /// <typeparam name="TResponse">The JSON response type.</typeparam>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="payload">Payload encoded as application/x\-www\-form\-urlencoded.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns>The deserialized response, or <c>null</c> if the operation returns <c>null</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the external service request fails.</exception>
    public virtual async Task<TResponse?> PostUrlEncodedAsync<TResponse>(string endpoint,
                                                                         object payload,
                                                                         Dictionary<string, string>? headers = null,
                                                                         CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(UnsafeMethodPolicy,
                                            endpoint,
                                            HttpMethod.Post,
                                            async (req, ct) =>
                                            {
                                                using IFlurlResponse res = await req
                                                    .PostUrlEncodedAsync(payload, cancellationToken: ct)
                                                    .ConfigureAwait(false);

                                                TResponse data = await res.GetJsonAsync<TResponse>()
                                                                          .ConfigureAwait(false);

                                                return (data, res.StatusCode);
                                            },
                                            headers,
                                            null,
                                            null,
                                            cancellationToken);
    }

    /// <summary>
    /// Sends a PUT request with a JSON payload and deserializes the response body as JSON.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The JSON response type.</typeparam>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="payload">Payload serialized to JSON request body.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns>The deserialized response, or <c>null</c> if the operation returns <c>null</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the external service request fails.</exception>
    public virtual async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint,
                                                                        TRequest payload,
                                                                        Dictionary<string, string>? headers = null,
                                                                        CancellationToken cancellationToken = default)
    {
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
                                            null,
                                            null,
                                            cancellationToken);
    }

    /// <summary>
    /// Sends a PATCH request with a JSON payload and deserializes the response body as JSON.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The JSON response type.</typeparam>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="payload">Payload serialized to JSON request body.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns>The deserialized response, or <c>null</c> if the operation returns <c>null</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the external service request fails.</exception>
    public virtual async Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint,
                                                                          TRequest payload,
                                                                          Dictionary<string, string>? headers = null,
                                                                          CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(UnsafeMethodPolicy,
                                            endpoint,
                                            HttpMethod.Patch,
                                            async (req, ct) =>
                                            {
                                                using IFlurlResponse res = await req
                                                    .PatchJsonAsync(payload, cancellationToken: ct)
                                                    .ConfigureAwait(false);

                                                TResponse data = await res.GetJsonAsync<TResponse>()
                                                                          .ConfigureAwait(false);

                                                return (data, res.StatusCode);
                                            },
                                            headers,
                                            null,
                                            null,
                                            cancellationToken);
    }

    /// <summary>
    /// Sends a DELETE request to the specified endpoint.
    /// </summary>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns><c>true</c> if the HTTP response indicates success; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the external service request fails.</exception>
    public virtual async Task<bool> DeleteAsync(string endpoint,
                                                Dictionary<string, string>? headers = null,
                                                CancellationToken cancellationToken = default)
    {
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
                                            null,
                                            null,
                                            cancellationToken);
    }

    /// <summary>
    /// Sends a HEAD request to the specified endpoint.
    /// </summary>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns><c>true</c> if the HTTP response indicates success; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the external service request fails.</exception>
    public virtual async Task<bool> HeadAsync(string endpoint,
                                              Dictionary<string, string>? headers = null,
                                              CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(SafeMethodPolicy,
                                            endpoint,
                                            HttpMethod.Head,
                                            async (req, ct) =>
                                            {
                                                using IFlurlResponse res = await req.HeadAsync(cancellationToken: ct)
                                                    .ConfigureAwait(false);

                                                return (res.ResponseMessage.IsSuccessStatusCode, res.StatusCode);
                                            },
                                            headers,
                                            null,
                                            null,
                                            cancellationToken);
    }

    #region ========== *** Protected Core Engine *** ==========

    /// <summary>
    /// Executes a Flurl request using a Polly policy, applying headers and optional request configuration.
    /// Converts <see cref="FlurlHttpException"/> into <see cref="ExternalServiceHttpException"/> with enriched context.
    /// </summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="policy">Polly policy used to execute the operation.</param>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="method">HTTP method for diagnostics and exception context.</param>
    /// <param name="operation">Delegate that performs the request and returns a tuple of (result, status code).</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="configurator">Optional delegate to further configure the request (e.g., attach auth tokens) prior to execution.</param>
    /// <param name="onUnauthorized">
    /// Optional delegate stored in the policy context (via <c>FlurlHttpClientFactory.RefreshFuncKey</c>)
    /// for token refresh or other 401 handling.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the policy and request execution.</param>
    /// <returns>The operation result, or <c>null</c> if the operation returns <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="policy"/>, <paramref name="method"/>, or <paramref name="operation"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the external service request fails.</exception>
    protected async Task<T?> ExecuteWithPolicyAsync<T>(IAsyncPolicy policy,
                                                       string endpoint,
                                                       HttpMethod method,
                                                       Func<IFlurlRequest, CancellationToken,
                                                           Task<(T? Result, int StatusCode)>> operation,
                                                       Dictionary<string, string>? headers = null,
                                                       Func<IFlurlRequest, CancellationToken, Task>? configurator =
                                                           null,
                                                       Func<CancellationToken, Task>? onUnauthorized = null,
                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(method);

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint must be provided.", nameof(endpoint));
        }

        Context context = new()
                          {
                              ["Lob"] = LobContext.LobName,
                              [FlurlHttpClientFactory.RefreshFuncKey] = onUnauthorized
                          };

        return await policy.ExecuteAsync<T?>(async (_, ct) =>
                                             {
                                                 string methodStr = method.Method;
                                                 string fullUrl = CombineUrl(_client.BaseUrl, endpoint);

                                                 try
                                                 {
                                                     LogRequest(methodStr, fullUrl);

                                                     IFlurlRequest request = _client.Request(endpoint);
                                                     ApplyHeaders(request, headers);

                                                     // Children (like GenesysApiClient) use this to inject tokens
                                                     if (configurator != null)
                                                     {
                                                         await configurator(request, ct).ConfigureAwait(false);
                                                     }

                                                     (T? result, int statusCode) =
                                                         await operation(request, ct).ConfigureAwait(false);

                                                     LogResponse(statusCode, methodStr, fullUrl);

                                                     return result;
                                                 }
                                                 catch (FlurlHttpException ex)
                                                 {
                                                     string? responseBody = await TryGetResponseStringAsync(ex)
                                                         .ConfigureAwait(false);

                                                     HttpStatusCode statusCode = ex.StatusCode != null
                                                         ? (HttpStatusCode)ex.StatusCode
                                                         : HttpStatusCode.InternalServerError;

                                                     ExternalServiceHttpException wrappedEx = new(
                                                         statusCode,
                                                         methodStr,
                                                         fullUrl,
                                                         $"External service request failed: {methodStr} {fullUrl}",
                                                         ex,
                                                         responseBody);

                                                     _logger.LogError(wrappedEx,
                                                                      "[LOB: {Lob}] {Method} failed | Status: {Status} | Response: {Body} | Exception: {ExJson}",
                                                                      LobContext.LobName,
                                                                      methodStr,
                                                                      statusCode,
                                                                      responseBody,
                                                                      wrappedEx.ToJson());

                                                     throw wrappedEx;
                                                 }
                                                 catch (Exception ex)
                                                 {
                                                     _logger.LogError(ex,
                                                                      "[LOB: {Lob}] {Method} failed | Exception: {ExJson}",
                                                                      LobContext.LobName,
                                                                      methodStr,
                                                                      ex.ToJson());

                                                     throw;
                                                 }
                                             },
                                             context,
                                             cancellationToken);
    }

    #endregion

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Applies the provided headers to the Flurl request.
    /// </summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="headers">Headers to apply, if any.</param>
    private static void ApplyHeaders(IFlurlRequest request, Dictionary<string, string>? headers)
    {
        if (headers == null || headers.Count == 0) return;

        foreach (KeyValuePair<string, string> header in headers)
        {
            request.WithHeader(header.Key, header.Value);
        }
    }

    /// <summary>
    /// Attempts to extract the response body as a string from a <see cref="FlurlHttpException"/>.
    /// </summary>
    /// <param name="ex">The Flurl exception.</param>
    /// <returns>The response body string if available; otherwise <c>null</c>.</returns>
    private static async Task<string?> TryGetResponseStringAsync(FlurlHttpException ex)
    {
        try
        {
            return await ex.GetResponseStringAsync().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Combines a base URL and endpoint into a normalized absolute URL for diagnostics/logging.
    /// </summary>
    /// <param name="baseUrl">Base URL.</param>
    /// <param name="endpoint">Relative endpoint.</param>
    /// <returns>Combined URL.</returns>
    private static string CombineUrl(string baseUrl, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return endpoint;

        return string.IsNullOrWhiteSpace(endpoint) ? baseUrl : $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
    }

    private void LogRequest(string method, string url)
    {
        _logger.LogDebug("HTTP Request | Method: {Method} | Url: {Url}", method, url);
    }

    private void LogResponse(int? statusCode, string method, string url)
    {
        _logger.LogDebug("HTTP Response | Status: {StatusCode} | Method: {Method} | Url: {Url}",
                         statusCode,
                         method,
                         url);
    }

    #endregion
}
