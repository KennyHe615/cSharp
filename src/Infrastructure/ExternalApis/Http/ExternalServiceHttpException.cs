using System.Net;


namespace Infrastructure.ExternalApis.Http;

/// <summary>
/// Represents a failed outbound HTTP call to an external service with enriched operation context.
/// </summary>
public sealed class ExternalServiceHttpException : InfrastructureException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceHttpException"/> class.
    /// </summary>
    /// <param name="statusCode">HTTP status code returned by the upstream service, if available.</param>
    /// <param name="method">HTTP method used by the request.</param>
    /// <param name="url">Absolute or relative request URL.</param>
    /// <param name="message">Exception message.</param>
    /// <param name="innerException">Inner exception.</param>
    /// <param name="responseSummary">
    /// Sanitized response summary (for example length/hash or a redacted snippet), not raw sensitive payload.
    /// </param>
    /// <param name="operationName">Optional logical operation name.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="method"/>, <paramref name="url"/>, or <paramref name="message"/> is empty.
    /// </exception>
    public ExternalServiceHttpException(HttpStatusCode? statusCode,
                                        string method,
                                        string url,
                                        string message,
                                        Exception? innerException = null,
                                        string? responseSummary = null,
                                        string? operationName = null) : base(message, innerException)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException("HTTP method must be provided.", nameof(method));
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Request URL must be provided.", nameof(url));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Exception message must be provided.", nameof(message));
        }

        StatusCode = statusCode;
        Method = method.Trim().ToUpperInvariant();
        Url = url.Trim();
        ResponseSummary = responseSummary;
        OperationName = string.IsNullOrWhiteSpace(operationName) ? null : operationName.Trim();
    }

    /// <summary>
    /// Gets the HTTP status code returned by the upstream service, if available.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// Gets the HTTP method used for the request (normalized uppercase).
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Gets the request URL.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets a sanitized summary of the response content, if available.
    /// </summary>
    public string? ResponseSummary { get; }

    /// <summary>
    /// Gets the logical operation name for the HTTP call, if provided.
    /// </summary>
    public string? OperationName { get; }
}
