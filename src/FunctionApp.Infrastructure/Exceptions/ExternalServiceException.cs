namespace FunctionApp.Infrastructure.Exceptions;

public abstract class ExternalServiceException(string message,
                                               Exception? innerException = null)
    : InfrastructureException(message, innerException);
