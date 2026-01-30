namespace Infrastructure.Exceptions;

public sealed class DatabaseException(string message,
                                      Exception inner) : InfrastructureException(message, inner);

public sealed class DatabaseSchemaMismatchException(string message,
                                                    Exception inner) : InfrastructureException(message, inner);
