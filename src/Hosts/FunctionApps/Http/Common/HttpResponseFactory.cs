using System.Net;

using Microsoft.Azure.Functions.Worker.Http;


namespace FunctionApps.Http.Common;

/// <summary>
/// Centralized factory for standard HTTP JSON responses used by FunctionApps HTTP triggers.
/// </summary>
public static class HttpResponseFactory
{
    /// <summary>
    /// Creates a 201 Created response with a JSON body.
    /// </summary>
    public static async Task<HttpResponseData> CreatedAsync(HttpRequestData req,
                                                            object payload,
                                                            CancellationToken ct = default)
    {
        HttpResponseData response = req.CreateResponse(HttpStatusCode.Created);

        await response.WriteAsJsonAsync(payload, ct)
                      .ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// Creates a 202 Accepted response with a JSON body.
    /// </summary>
    public static async Task<HttpResponseData> AcceptedAsync(HttpRequestData req,
                                                             object payload,
                                                             CancellationToken ct = default)
    {
        HttpResponseData response = req.CreateResponse(HttpStatusCode.Accepted);

        await response.WriteAsJsonAsync(payload, ct)
                      .ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// Creates a 400 Bad Request response with a standardized error payload.
    /// </summary>
    public static async Task<HttpResponseData> BadRequestAsync(HttpRequestData req,
                                                               string error,
                                                               CancellationToken ct = default)
    {
        return await WriteErrorAsync(req,
                                     HttpStatusCode.BadRequest,
                                     error,
                                     ct)
                              .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a 500 Internal Server Error response with a standardized error payload.
    /// </summary>
    public static async Task<HttpResponseData> InternalServerErrorAsync(HttpRequestData req,
                                                                        string error,
                                                                        CancellationToken ct = default)
    {
        return await WriteErrorAsync(req,
                                     HttpStatusCode.InternalServerError,
                                     error,
                                     ct)
                              .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    private static async Task<HttpResponseData> WriteErrorAsync(HttpRequestData req,
                                                                HttpStatusCode statusCode,
                                                                string error,
                                                                CancellationToken ct)
    {
        HttpResponseData response = req.CreateResponse(statusCode);

        await response.WriteAsJsonAsync(new { Error = error }, ct)
                      .ConfigureAwait(false);

        return response;
    }

    #endregion
}
