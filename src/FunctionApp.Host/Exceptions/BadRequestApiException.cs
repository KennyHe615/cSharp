using System.Net;


namespace FunctionApp.Host.Exceptions;

public sealed class BadRequestApiException(string message,
                                           string? errorCode = null) : ApiException(HttpStatusCode.BadRequest, message)
{
    public override string? ErrorCode { get; } = errorCode;
}
