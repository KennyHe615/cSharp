using Application.Abstractions.Context;
using Application.Abstractions.Identity;

using Infrastructure.Configuration.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SharedKernel.Environment;
using SharedKernel.Lobs;
using SharedKernel.Logging;


namespace Infrastructure.Identity;

/// <summary>
/// Resolves LOB-scoped credentials from Key Vault and populates them into
/// <see cref="ILobContextAccessor"/> for downstream runtime usage.
/// </summary>
/// <remarks>
/// Secret names are built using the convention:
/// <c>{Prefix}-{EnvironmentAlias}-{Lob}</c>.
/// Required credentials are fetched in parallel.
/// </remarks>
/// <param name="secretProvider">Secret provider used to access Key Vault values.</param>
/// <param name="keyVaultOptions">Key Vault naming options.</param>
/// <param name="appEnvironment">Normalized application environment context.</param>
/// <param name="logger">Logger instance.</param>
public sealed class LobCredentialProvider(ISecretProvider secretProvider,
                                          IOptions<KeyVaultOptions> keyVaultOptions,
                                          AppEnvironment appEnvironment,
                                          ILogger<LobCredentialProvider> logger) : ICredentialProvider
{
    private const string LogCategory = "CredentialResolution";

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="accessor"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when LOB name is missing or any required secret is missing/empty.
    /// </exception>
    public async Task PopulateAsync(ILobContextAccessor accessor, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        const string logEntity = "Populate";
        string? lob = accessor.LobName;
        if (string.IsNullOrWhiteSpace(lob))
        {
            throw new InvalidOperationException("LobName must be set before resolving credentials.");
        }

        using IDisposable scope = logger.BeginOperationScope(new LobName(lob), LogCategory);

        (string clientIdSecretName, string clientSecretName, string dbConnSecretName) = BuildSecretNames(lob);

        (string clientId, string clientSecret, string dbConnectionString) =
            await ResolveCredentialValuesAsync(clientIdSecretName,
                                               clientSecretName,
                                               dbConnSecretName,
                                               ct)
               .ConfigureAwait(false);

        ApplyCredentials(accessor,
                         clientId,
                         clientSecret,
                         dbConnectionString);

        logger.LogInformation(LobLogTemplates.LobCategoryEntity + "Successfully populated runtime credentials.",
                              lob,
                              LogCategory,
                              logEntity);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Builds all required Key Vault secret names for the given LOB.
    /// </summary>
    private (string clientIdSecretName, string clientSecretName, string dbConnSecretName) BuildSecretNames(string lob)
    {
        KeyVaultOptions options = keyVaultOptions.Value;
        string env = appEnvironment.Alias;
        const string logEntity = "BuildSecretNames";

        string clientIdSecretName = BuildLobSecretName(options.GenesysClientIdSecretPrefix, env, lob);
        string clientSecretName = BuildLobSecretName(options.GenesysClientSecretSecretPrefix, env, lob);
        string dbConnSecretName = BuildLobSecretName(options.LandingDbConnStrSecretPrefix, env, lob);

        logger.LogInformation(LobLogTemplates.LobCategoryEntity
                              + "Resolving required credentials from Key Vault in environment '{EnvironmentAlias}'.",
                              lob,
                              LogCategory,
                              logEntity,
                              env);

        return (clientIdSecretName, clientSecretName, dbConnSecretName);
    }

    /// <summary>
    /// Resolves all required credential values in parallel.
    /// </summary>
    private async Task<(string clientId, string clientSecret, string dbConnectionString)> ResolveCredentialValuesAsync(
        string clientIdSecretName,
        string clientSecretName,
        string dbConnSecretName,
        CancellationToken ct)
    {
        Task<string> clientIdTask = GetRequiredSecretAsync(clientIdSecretName, ct);
        Task<string> clientSecretTask = GetRequiredSecretAsync(clientSecretName, ct);
        Task<string> dbConnTask = GetRequiredSecretAsync(dbConnSecretName, ct);

        await Task.WhenAll(clientIdTask, clientSecretTask, dbConnTask)
                  .ConfigureAwait(false);

        return (clientIdTask.Result, clientSecretTask.Result, dbConnTask.Result);
    }

    /// <summary>
    /// Resolves a required secret and validates that it is non-empty.
    /// </summary>
    /// <param name="secretName">Secret name to fetch from Key Vault.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Non-empty secret value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the secret is null/empty/whitespace.</exception>
    private async Task<string> GetRequiredSecretAsync(string secretName, CancellationToken ct)
    {
        string value = await secretProvider.GetSecretAsync(secretName, ct)
                                           .ConfigureAwait(false);

        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Key Vault secret '{secretName}' is missing or empty.");
    }

    /// <summary>
    /// Copies resolved credential values into the accessor.
    /// </summary>
    private static void ApplyCredentials(ILobContextAccessor accessor,
                                         string clientId,
                                         string clientSecret,
                                         string dbConnectionString)
    {
        accessor.GenesysClientId = clientId;
        accessor.GenesysClientSecret = clientSecret;
        accessor.DbConnectionString = dbConnectionString;
    }

    /// <summary>
    /// Builds a LOB-specific secret name following the convention:
    /// <c>{Prefix}-{EnvironmentAlias}-{Lob}</c>.
    /// </summary>
    private static string BuildLobSecretName(string prefix, string environmentAlias, string lob)
    {
        return $"{prefix}-{lob}-{environmentAlias}";
    }

    #endregion
}
