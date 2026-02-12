using Application.Common.Abstractions.Context;
using Application.Common.Abstractions.Providers;

using Microsoft.Extensions.Logging;

using Shared.Constants;


namespace Infrastructure.Shared.Providers;

/// <summary>
/// Implementation of <see cref="ILobSecretsResolver"/> that fetches secrets from an <see cref="ISecretProvider"/>
/// and populates the <see cref="ILobContextAccessor"/>.
/// </summary>
/// <param name="secretProvider">The underlying secret provider (e.g., Key Vault).</param>
/// <param name="logger">The logger instance.</param>
public sealed class LobSecretsResolver(ISecretProvider secretProvider,
                                       ILogger<LobSecretsResolver> logger) : ILobSecretsResolver
{
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="accessor"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the LOB name is missing or a required secret cannot be resolved.</exception>
    public async Task PopulateAsync(ILobContextAccessor accessor, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        string? lob = accessor.LobName;

        if (string.IsNullOrWhiteSpace(lob))
        {
            throw new InvalidOperationException("LobName must be set before resolving secrets.");
        }

        Task<string> clientIdTask = GetRequiredSecretAsync($"{KeyVaultsConstants.GenesysClientId}-{lob}", ct);
        Task<string> clientSecretTask = GetRequiredSecretAsync($"{KeyVaultsConstants.GenesysClientSecret}-{lob}", ct);
        Task<string> dbConnTask = GetRequiredSecretAsync($"{KeyVaultsConstants.LandingDbConnStr}-{lob}", ct);

        await Task.WhenAll(clientIdTask, clientSecretTask, dbConnTask).ConfigureAwait(false);

        accessor.GenesysClientId = clientIdTask.Result;
        accessor.GenesysClientSecret = clientSecretTask.Result;
        accessor.DbConnStr = dbConnTask.Result;

        logger.LogDebug(CommonConstants.LobLogPrefix + "✅ Secrets populated successfully", lob);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Retrieves a secret and validates that it is not null or whitespace.
    /// </summary>
    /// <param name="secretName">The name of the secret to fetch.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The resolved secret value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the secret is missing or empty.</exception>
    private async Task<string> GetRequiredSecretAsync(string secretName, CancellationToken ct)
    {
        string value = await secretProvider.GetSecretAsync(secretName, ct).ConfigureAwait(false);

        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Key Vault secret `{secretName}` is missing or empty.");
    }

    #endregion
}
