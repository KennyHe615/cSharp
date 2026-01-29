using Infrastructure.Exceptions;


namespace Infrastructure.Azure.KeyVaults;

public sealed class KeyVaultsException : InfrastructureException
{
    public KeyVaultsException()
    {
    }

    public KeyVaultsException(string message) : base(message)
    {
    }

    public KeyVaultsException(string message, Exception inner) : base(message, inner)
    {
    }
}
