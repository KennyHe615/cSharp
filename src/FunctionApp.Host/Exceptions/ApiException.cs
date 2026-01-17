using System.Net;


namespace FunctionApp.Host.Exceptions;

public abstract class ApiException(HttpStatusCode statusCode,
                                   string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public virtual string? ErrorCode => null;
}
