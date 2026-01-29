namespace Infrastructure.Exceptions;

public abstract class ExternalServiceException(string message,
                                               Exception? inner = null) : InfrastructureException(message, inner);
