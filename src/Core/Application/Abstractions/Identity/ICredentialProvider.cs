using Application.Abstractions.Context;


namespace Application.Abstractions.Identity;

/// <summary>
/// Resolves and populates runtime credentials for a specific LOB context.
/// </summary>
public interface ICredentialProvider
{
    /// <summary>
    /// Populates credential fields on the provided LOB context accessor.
    /// </summary>
    Task PopulateAsync(ILobContextAccessor accessor, CancellationToken ct = default);
}
