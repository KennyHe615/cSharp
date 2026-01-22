namespace FunctionApp.Infrastructure.Exceptions;

public sealed class KeyVaultException : InfrastructureException
{
    public KeyVaultException()
    {
    }

    public KeyVaultException(string message) : base(message)
    {
    }

    public KeyVaultException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
