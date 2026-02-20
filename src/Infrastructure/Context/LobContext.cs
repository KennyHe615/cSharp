using Application.Abstractions.Context;

using SharedKernel.Lobs;


namespace Infrastructure.Context;

/// <summary>
/// Implementation of <see cref="ILobContextAccessor"/> that retrieves LOB-specific data from an <paramref name="accessor"/>.
/// </summary>
/// <param name="accessor">The accessor used to retrieve the current LOB state.</param>
/// <remarks>
/// All properties perform validation and throw InvalidOperationException if accessed before the context is properly initialized.
/// </remarks>
public sealed class LobContext(ILobContextAccessor accessor) : ILobContext
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when accessed before the LOB name is set in the accessor.</exception>
    public LobName LobName =>
        new LobName(accessor.LobName ?? throw new InvalidOperationException("Missing LobName in context."));

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
    public string DbConnectionString =>
        !string.IsNullOrWhiteSpace(accessor.DbConnectionString)
            ? accessor.DbConnectionString!
            : throw new InvalidOperationException($"Missing DatabaseConnectionString for LOB `{LobName}`.");
}
