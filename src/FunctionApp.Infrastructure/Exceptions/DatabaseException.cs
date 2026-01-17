namespace FunctionApp.Infrastructure.Exceptions;

public sealed class DatabaseException(string message,
                                      Exception innerException) : InfrastructureException(message, innerException);
