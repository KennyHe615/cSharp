using Application.Common.Abstractions.Context;


namespace Infrastructure.Shared.Context;

/// <summary>
/// Default implementation of <see cref="ILobContextAccessor"/> for managing LOB-specific state.
/// </summary>
public sealed class LobContextAccessor : ILobContextAccessor
{
    /// <inheritdoc />
    public string? LobName { get; set; }

    /// <inheritdoc />
    public string? GenesysClientId { get; set; }

    /// <inheritdoc />
    public string? GenesysClientSecret { get; set; }

    /// <inheritdoc />
    public string? DbConnStr { get; set; }
}
