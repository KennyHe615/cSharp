using Application.Abstractions.Planning;
using Application.Enums;

using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.Planning;

/// <summary>
/// Factory for resolving analytics hit-count providers by sync category.
/// </summary>
/// <param name="serviceProvider">Service provider used to resolve concrete provider instances.</param>
public sealed class HitCountProviderFactory(IServiceProvider serviceProvider) : IHitCountProviderFactory
{
    /// <inheritdoc />
    /// <exception cref="NotSupportedException">
    /// Thrown when the category does not support analytics hit-count planning.
    /// </exception>
    public IHitCountProvider Create(SyncAnalyticsCategory category)
    {
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return category switch
               {
                   SyncAnalyticsCategory.UsersDetails => serviceProvider
                          .GetRequiredService<UsersDetailsHitCountProvider>(),
                   SyncAnalyticsCategory.ConversationsDetails => serviceProvider
                          .GetRequiredService<ConversationsDetailsHitCountProvider>(),
                   _ => throw new
                                NotSupportedException($"Sync category '{category}' does not support analytics hit-count planning.")
               };
    }
}
