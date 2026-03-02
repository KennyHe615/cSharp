using Application.Common.Abstractions.Providers;
using Application.Common.Enums;


namespace Application.Common.Abstractions.Factories;

/// <summary>
/// Defines a factory for creating hit count providers based on synchronization category.
/// </summary>
public interface IHitCountProviderFactory
{
    /// <summary>
    /// Creates a hit count provider for the specified synchronization category.
    /// </summary>
    /// <param name="category">The synchronization category to create a provider for.</param>
    /// <returns>An implementation of <see cref="IHitCountProvider"/> for the specified category.</returns>
    IHitCountProvider Create(SyncCategory category);
}
