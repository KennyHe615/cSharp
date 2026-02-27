namespace Application.Abstractions.Context;

/// <summary>
/// Provides a mechanism to store and access Line of Business (LOB) specific context information across the application.
/// </summary>
/// <remarks>
/// This accessor is typically used as a scoped service to hold state for the duration of a synchronization run or request.
/// </remarks>
public interface ILobContextAccessor
{
    /// <summary>
    /// Gets or sets the name of the current Line of Business.
    /// </summary>
    string? LobName { get; set; }

    /// <summary>
    /// Gets or sets the Genesys Cloud OAuth Client ID for the current LOB.
    /// </summary>
    string? GenesysClientId { get; set; }

    /// <summary>
    /// Gets or sets the Genesys Cloud OAuth Client Secret for the current LOB.
    /// </summary>
    string? GenesysClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the database connection string for the current LOB.
    /// </summary>
    string? DbConnectionString { get; set; }
}
