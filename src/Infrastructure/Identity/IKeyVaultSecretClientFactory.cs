using Azure.Security.KeyVault.Secrets;


namespace Infrastructure.Identity;

public interface IKeyVaultSecretClientFactory
{
    SecretClient Create();
}
