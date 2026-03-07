using System.Net;

using Microsoft.Azure.Functions.Worker.Http;


namespace FunctionApp.Http.Common;

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
    /// Creates a 400 Bad Request response with a standardized error payload.
    /// </summary>
    public static async Task<HttpResponseData> BadRequestAsync(HttpRequestData req,
                                                               string error,
                                                               CancellationToken ct = default)
    {
        HttpResponseData response = req.CreateResponse(HttpStatusCode.BadRequest);

        await response.WriteAsJsonAsync(new { Error = error }, ct)
                      .ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// Creates a 500 Internal Server Error response with a standardized error payload.
    /// </summary>
    public static async Task<HttpResponseData> InternalServerErrorAsync(HttpRequestData req,
                                                                        string error,
                                                                        CancellationToken ct = default)
    {
        HttpResponseData response = req.CreateResponse(HttpStatusCode.InternalServerError);

        await response.WriteAsJsonAsync(new { Error = error }, ct)
                      .ConfigureAwait(false);

        return response;
    }
}
