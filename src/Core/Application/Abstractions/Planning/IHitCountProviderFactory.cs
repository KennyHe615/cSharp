using Application.Enums;


namespace Application.Abstractions.Planning;

/// <summary>
/// Resolves analytics hit-count providers by <see cref="SyncAnalyticsCategory"/>.
/// </summary>
public interface IHitCountProviderFactory
{
    /// <summary>
    /// Creates a provider instance for the specified analytics category.
    /// </summary>
    /// <param name="category">Sync analytics category to resolve.</param>
    /// <returns>A hit-count provider for the requested analytics category.</returns>
    IHitCountProvider Create(SyncAnalyticsCategory category);
}
