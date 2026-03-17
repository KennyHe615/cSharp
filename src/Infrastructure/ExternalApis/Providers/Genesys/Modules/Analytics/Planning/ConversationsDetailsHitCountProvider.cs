using Application.Abstractions.Planning;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.Planning;

/// <summary>
/// Hit-count provider for Conversations Details analytics category.
/// Placeholder during migration until conversations details client is wired.
/// </summary>
public sealed class ConversationsDetailsHitCountProvider : IHitCountProvider
{
    public Task<int> GetHitCountAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default)
    {
        if (end <= start)
        {
            throw new ArgumentException("End must be greater than start.", nameof(end));
        }

        // TODO: ConversationsDetails hit-count provider
        return Task.FromResult(-1);
        // throw new
        //     NotSupportedException("ConversationsDetails hit-count provider is not wired yet. Implement endpoint client integration first.");
    }
}
