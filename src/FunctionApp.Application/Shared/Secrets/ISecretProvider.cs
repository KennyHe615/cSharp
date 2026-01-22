namespace FunctionApp.Application.Shared.Secrets;

public interface ISecretProvider
{
    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

    Task UpsertSecretAsync(string secretName, string value, CancellationToken cancellationToken = default);

    Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);
}
