using System.Diagnostics;

using Flurl.Http;

using FunctionApp.Application.Shared.Context;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Wrap;


namespace FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

public class FlurlHttpClient : IFlurlHttpClient
{
    #region ========== *** Properties & Constructor *** ==========

    private readonly FlurlClient _client;
    private readonly ILogger _logger;
    protected readonly ILobContext LobContext;
    private readonly Func<CancellationToken, Task<string?>>? _tokenProviderFunc;
    private readonly Func<CancellationToken, Task>? _refreshTokenFunc;
    private readonly AsyncPolicyWrap _safeMethodPolicy;
    private readonly AsyncPolicyWrap _unsafeMethodPolicy;

    protected FlurlHttpClient(FlurlClient client,
                              IFlurlHttpClientFactory factory,
                              ILobContext lobContext,
                              ILogger logger,
                              Func<CancellationToken, Task<string?>>? tokenProviderFunc = null,
                              Func<CancellationToken, Task>? refreshTokenFunc = null)
    {
        _client = client;
        LobContext = lobContext;
        _logger = logger;
        _tokenProviderFunc = tokenProviderFunc;
        _refreshTokenFunc = refreshTokenFunc;

        // Shared global policies
        _safeMethodPolicy = factory.GetSafePolicy();
        _unsafeMethodPolicy = factory.GetUnsafePolicy();

        // 3. Register standard event handlers
        // _client.BeforeCall(LogRequest);
        // _client.AfterCall(LogResponse);
        // // _client.OnError(HandleError); // Error handling is done in ExecuteWithPolicyAsync
    }

    public string BaseUrl => _client.BaseUrl;

    #endregion

    public async Task<T?> GetAsync<T>(string endpoint,
                                      Dictionary<string, string>? headers = null,
                                      CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(_safeMethodPolicy,
                                            async (token, ct) =>
                                            {
                                                IFlurlRequest request = _client.Request(endpoint);

                                                ApplyHeadersAndAuth(request, headers, token);

                                                using IFlurlResponse? resp = await request
                                                    .GetAsync(cancellationToken: ct)
                                                    .ConfigureAwait(false);
                                                T data = await resp.GetJsonAsync<T>().ConfigureAwait(false);

                                                return (data, resp.StatusCode);
                                            },
                                            endpoint,
                                            "GET",
                                            cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint,
                                                                 TRequest payload,
                                                                 Dictionary<string, string>? headers = null,
                                                                 CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(_unsafeMethodPolicy,
                                            async (token, ct) =>
                                            {
                                                IFlurlRequest request = _client.Request(endpoint);

                                                ApplyHeadersAndAuth(request, headers, token);

                                                using IFlurlResponse? resp = await request
                                                    .PostJsonAsync(payload, cancellationToken: ct)
                                                    .ConfigureAwait(false);
                                                TResponse data = await resp.GetJsonAsync<TResponse>()
                                                                           .ConfigureAwait(false);

                                                return (data, resp.StatusCode);
                                            },
                                            endpoint,
                                            "POST",
                                            cancellationToken);
    }

    public async Task<TResponse?> PostUrlEncodedAsync<TResponse>(string endpoint,
                                                                 object payload,
                                                                 Dictionary<string, string>? headers = null,
                                                                 CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(_unsafeMethodPolicy,
                                            async (token, ct) =>
                                            {
                                                IFlurlRequest request = _client.Request(endpoint);

                                                ApplyHeadersAndAuth(request, headers, token);

                                                using IFlurlResponse? resp = await request
                                                    .PostUrlEncodedAsync(payload, cancellationToken: ct)
                                                    .ConfigureAwait(false);
                                                TResponse data = await resp.GetJsonAsync<TResponse>()
                                                                           .ConfigureAwait(false);

                                                return (data, resp.StatusCode);
                                            },
                                            endpoint,
                                            "POST",
                                            cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint,
                                                                TRequest payload,
                                                                Dictionary<string, string>? headers = null,
                                                                CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(_unsafeMethodPolicy,
                                            async (token, ct) =>
                                            {
                                                IFlurlRequest request = _client.Request(endpoint);

                                                ApplyHeadersAndAuth(request, headers, token);

                                                using IFlurlResponse? resp = await request
                                                    .PutJsonAsync(payload, cancellationToken: ct)
                                                    .ConfigureAwait(false);
                                                TResponse data = await resp.GetJsonAsync<TResponse>()
                                                                           .ConfigureAwait(false);

                                                return (data, resp.StatusCode);
                                            },
                                            endpoint,
                                            "PUT",
                                            cancellationToken);
    }

    public async Task<bool> DeleteAsync(string endpoint,
                                        Dictionary<string, string>? headers = null,
                                        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(_unsafeMethodPolicy,
                                            async (token, ct) =>
                                            {
                                                IFlurlRequest request = _client.Request(endpoint);

                                                ApplyHeadersAndAuth(request, headers, token);

                                                using IFlurlResponse resp = await request
                                                    .DeleteAsync(cancellationToken: ct)
                                                    .ConfigureAwait(false);

                                                return (resp.ResponseMessage.IsSuccessStatusCode, resp.StatusCode);
                                            },
                                            endpoint,
                                            "DELETE",
                                            cancellationToken);
    }

    #region ========== *** Private Methods *** ==========

    private static void ApplyHeadersAndAuth(IFlurlRequest request, Dictionary<string, string>? headers, string? token)
    {
        bool hasExternalAuth = false;

        if (headers != null)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                request.WithHeader(header.Key, header.Value);
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    hasExternalAuth = true;
                }
            }
        }

        if (!hasExternalAuth && !string.IsNullOrEmpty(token))
        {
            request.WithOAuthBearerToken(token);
        }
    }

    private async Task<T?> ExecuteWithPolicyAsync<T>(AsyncPolicyWrap policy,
                                                     Func<string?, CancellationToken, Task<(T? Result, int StatusCode)>>
                                                         operation,
                                                     string endpoint,
                                                     string method,
                                                     CancellationToken cancellationToken)
    {
        // Pass instance-specific state (LOB Name and Refresh Func) to the Singleton Policy via Context
        Context context = new()
                          {
                              ["Lob"] = LobContext.LobName,
                              [FlurlHttpClientFactory.RefreshFuncKey] = _refreshTokenFunc
                          };

        return await policy.ExecuteAsync(async (_, ct) =>
                                         {
                                             try
                                             {
                                                 LogRequest(method, endpoint);

                                                 string? currentToken = await GetTokenIfNeededAsync(ct);

                                                 (T? result, int statusCode) = await operation(currentToken, ct);

                                                 // Log Success
                                                 LogResponse(statusCode, method, endpoint);

                                                 return result;
                                             }
                                             catch (FlurlHttpException ex)
                                             {
                                                 // Detailed failure logging including LOB and Response Body
                                                 string responseBody = await ex.GetResponseStringAsync();

                                                 _logger.LogError(ex,
                                                                  "[LOB: {Lob}] {Method} request failed | Endpoint: {Endpoint} | Status: {StatusCode} | Response: {Body}",
                                                                  LobContext.LobName ?? "N/A",
                                                                  method,
                                                                  endpoint,
                                                                  ex.StatusCode,
                                                                  responseBody);

                                                 throw;
                                             }
                                             catch (OperationCanceledException)
                                             {
                                                 // Let cancellation propagate naturally (e.g., job suspension)
                                                 throw;
                                             }
                                             catch (Exception ex)
                                             {
                                                 _logger.LogError(ex,
                                                                  "[LOB: {Lob}] {Method} request failed | Endpoint: {Endpoint}",
                                                                  LobContext.LobName ?? "N/A",
                                                                  method,
                                                                  endpoint);

                                                 throw;
                                             }
                                         },
                                         context,
                                         cancellationToken);
    }

    private async Task<string?> GetTokenIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_tokenProviderFunc != null) return await _tokenProviderFunc(cancellationToken);

        return null;
    }

    private void LogRequest(string method, string endpoint)
    {
        string correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString()[..8];
        _logger.LogDebug("[LOB: {Lob}] HTTP Request [CorrelationId: {CorrelationId}]: {Method} {Url}",
                         LobContext.LobName ?? "N/A",
                         correlationId,
                         method,
                         endpoint);
    }

    private void LogResponse(int? statusCode, string method, string endpoint)
    {
        _logger.LogDebug("[LOB: {Lob}] HTTP Response: {StatusCode} for {Method} {Url}",
                         LobContext.LobName ?? "N/A",
                         statusCode,
                         method,
                         endpoint);
    }

    #endregion
}
