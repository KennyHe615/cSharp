namespace Infrastructure.Identity;

public sealed class KeyVaultSecretException : InfrastructureException
{
    public KeyVaultSecretException()
    {
    }

    public KeyVaultSecretException(string message) : base(message)
    {
    }

    public KeyVaultSecretException(string message, Exception inner) : base(message, inner)
    {
    }
}
