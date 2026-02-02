using Application.Shared.Context;


namespace Infrastructure.Shared.Context;

/// <summary>
/// Implementation of <see cref="ILobContext"/> that retrieves LOB-specific data from an <see cref="ILobContextAccessor"/>.
/// </summary>
/// <param name="accessor">The accessor used to retrieve the current LOB state.</param>
/// <remarks>
/// All properties perform validation and throw <see cref="InvalidOperationException"/> if accessed before the context is properly initialized.
/// </remarks>
public sealed class LobContext(ILobContextAccessor accessor) : ILobContext
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when accessed before the LOB name is set in the accessor.</exception>
    public string LobName =>
        !string.IsNullOrWhiteSpace(accessor.LobName)
            ? accessor.LobName!
            : throw new InvalidOperationException("LOB context was not initialized with a LobName.");

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when the Genesys Client ID is missing for the current LOB.</exception>
    public string GenesysClientId =>
        !string.IsNullOrWhiteSpace(accessor.GenesysClientId)
            ? accessor.GenesysClientId!
            : throw new InvalidOperationException($"Missing GenesysClientId for LOB `{LobName}`.");

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when the Genesys Client Secret is missing for the current LOB.</exception>
    public string GenesysClientSecret =>
        !string.IsNullOrWhiteSpace(accessor.GenesysClientSecret)
            ? accessor.GenesysClientSecret!
            : throw new InvalidOperationException($"Missing GenesysClientSecret for LOB `{LobName}`.");

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when the database connection string is missing for the current LOB.</exception>
    public string DbConnStr =>
        !string.IsNullOrWhiteSpace(accessor.DbConnStr)
            ? accessor.DbConnStr!
            : throw new InvalidOperationException($"Missing DatabaseConnectionString for LOB `{LobName}`.");
}
