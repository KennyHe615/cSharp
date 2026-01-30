namespace Application.Shared.Providers;

public interface ISecretProvider
{
    public Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

    public Task UpsertSecretAsync(string secretName, string value, CancellationToken cancellationToken = default);

    public Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);
}
