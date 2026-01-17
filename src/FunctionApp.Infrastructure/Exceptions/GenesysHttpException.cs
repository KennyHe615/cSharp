using System.Net;


namespace FunctionApp.Infrastructure.Exceptions;

public sealed class GenesysHttpException(HttpStatusCode statusCode,
                                         string message,
                                         Exception innerException) : ExternalServiceException(message, innerException)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
