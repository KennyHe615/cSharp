using Application.Common.Abstractions.Factories;
using Application.Common.Abstractions.Providers;
using Application.Common.Enums;

using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.ExternalServices.Genesys.Providers;

/// <summary>
/// Factory implementation for creating hit count providers based on synchronization category.
/// </summary>
public class HitCountProviderFactory(IServiceProvider serviceProvider) : IHitCountProviderFactory
{
    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Thrown when the specified category does not support hit count queries.</exception>
    public IHitCountProvider Create(SyncCategory category)
    {
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return category switch
        {
            SyncCategory.UserDetails => serviceProvider.GetRequiredService<UserDetailsHitCountProvider>(),
            // SyncCategory.ConversationDetails => serviceProvider.GetRequiredService<ConversationHitCountProvider>(),
            _ => throw new NotSupportedException($"Sync category '{category}' does not support hit count queries.")
        };
    }
}
