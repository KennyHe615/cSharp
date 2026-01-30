using System.Net;

using Infrastructure.Exceptions;


namespace Infrastructure.ExternalServices;

/// <summary>
/// Exception thrown when an external HTTP service call fails with a non-success status code
/// or otherwise needs to be represented as an infrastructure-level HTTP error.
/// </summary>
public sealed class ExternalServiceHttpException : InfrastructureException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceHttpException"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned by the external service.</param>
    /// <param name="method">The HTTP method used for the request (e.g., <c>GET</c>, <c>POST</c>).</param>
    /// <param name="url">The absolute or relative request URL.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="inner">The inner exception, if any.</param>
    /// <param name="responseBody">The response body returned by the external service, if available.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="method"/> or <paramref name="url"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <c>null</c>.</exception>
    public ExternalServiceHttpException(HttpStatusCode statusCode,
                                        string method,
                                        string url,
                                        string message,
                                        Exception? inner = null,
                                        string? responseBody = null) : base(
        message ?? throw new ArgumentNullException(nameof(message)),
        inner)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException("HTTP method must be provided.", nameof(method));
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Request URL must be provided.", nameof(url));
        }

        StatusCode = statusCode;
        Method = method;
        Url = url;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Gets the HTTP status code returned by the external service.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the HTTP method used for the request.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Gets the request URL.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets the response body returned by the external service, if available.
    /// </summary>
    public string? ResponseBody { get; }
}
