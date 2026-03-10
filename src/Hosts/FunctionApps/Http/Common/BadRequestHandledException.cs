using Microsoft.Azure.Functions.Worker.Http;


namespace FunctionApp.Http.Common;

/// <summary>
/// Exception used to short-circuit HTTP trigger flow when a 400 response has already been created.
/// </summary>
/// <remarks>
/// This allows shared request-parsing/validation helpers to prepare a standardized bad-request response
/// and signal the caller to return it directly without re-wrapping.
/// </remarks>
public sealed class BadRequestHandledException(HttpResponseData response) : Exception
{
    /// <summary>
    /// Gets the prebuilt HTTP response that should be returned to the caller.
    /// </summary>
    public HttpResponseData Response { get; } = response;
}
