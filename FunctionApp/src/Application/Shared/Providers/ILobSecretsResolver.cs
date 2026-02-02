using Application.Shared.Context;


namespace Application.Shared.Providers;

/// <summary>
/// Defines a resolver for populating Line of Business (LOB) specific secrets into a context accessor.
/// </summary>
public interface ILobSecretsResolver
{
    /// <summary>
    /// Resolves and populates secrets for the current LOB defined in the <paramref name="accessor"/>.
    /// </summary>
    /// <param name="accessor">The context accessor to be populated with resolved secret values.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous resolution operation.</returns>
    Task PopulateAsync(ILobContextAccessor accessor, CancellationToken ct = default);
}
