using System.Text.Json;

using Microsoft.Azure.Functions.Worker.Http;


namespace FunctionApp.Http.Common;

/// <summary>
/// Shared HTTP request parsing helpers for FunctionApps HTTP triggers.
/// </summary>
public static class HttpRequestParsers
{
    /// <summary>
    /// Tries to deserialize a JSON request body into <typeparamref name="TRequest"/>.
    /// Writes a standardized 400 response and throws <see cref="BadRequestHandledException"/>
    /// when the body is null/invalid for the expected payload type.
    /// </summary>
    /// <typeparam name="TRequest">Request model type.</typeparam>
    /// <param name="req">Incoming HTTP request.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deserialized request model.</returns>
    /// <exception cref="BadRequestHandledException">
    /// Thrown when body is null after deserialization; response is already prepared.
    /// </exception>
    /// <exception cref="JsonException">Thrown by System.Text.Json for malformed payloads.</exception>
    public static async Task<TRequest> DeserializeOrBadRequestAsync<TRequest>(HttpRequestData req,
                                                                              JsonSerializerOptions options,
                                                                              CancellationToken ct = default)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(options);

        TRequest? request = await JsonSerializer.DeserializeAsync<TRequest>(req.Body, options, ct)
                                                .ConfigureAwait(false);

        if (request is not null) return request;

        HttpResponseData response = await HttpResponseFactory.BadRequestAsync(req, "Invalid request body.", ct)
                                                             .ConfigureAwait(false);

        throw new BadRequestHandledException(response);
    }
}
