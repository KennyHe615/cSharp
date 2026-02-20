using SharedKernel.Lobs;


namespace Application.Abstractions.Context;

/// <summary>
/// Provides access to configuration and identity details for a specific Line of Business (LOB).
/// </summary>
public interface ILobContext
{
    /// <summary>
    /// Gets the unique name of the Line of Business.
    /// </summary>
    LobName LobName { get; }

    /// <summary>
    /// Gets the Genesys Cloud OAuth Client ID associated with this LOB.
    /// </summary>
    string GenesysClientId { get; }

    /// <summary>
    /// Gets the Genesys Cloud OAuth Client Secret associated with this LOB.
    /// </summary>
    string GenesysClientSecret { get; }

    /// <summary>
    /// Gets the database connection string for this LOB's dedicated storage.
    /// </summary>
    string DbConnectionString { get; }
}
