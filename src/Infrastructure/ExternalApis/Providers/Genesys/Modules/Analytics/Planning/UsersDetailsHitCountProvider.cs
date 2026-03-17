using Application.Abstractions.External;
using Application.Abstractions.Planning;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.Planning;

/// <summary>
/// Hit-count provider for Users Details analytics category.
/// </summary>
public sealed class UsersDetailsHitCountProvider(IAnalyticsUsersDetailsClient usersDetailsClient) : IHitCountProvider
{
    private readonly IAnalyticsUsersDetailsClient _usersDetailsClient =
        usersDetailsClient ?? throw new ArgumentNullException(nameof(usersDetailsClient));

    /// <inheritdoc />
    public async Task<int> GetHitCountAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default)
    {
        if (end <= start)
        {
            throw new ArgumentException("End must be greater than start.", nameof(end));
        }

        return await _usersDetailsClient.GetHitCountAsync(start, end, ct)
                                        .ConfigureAwait(false);
    }
}
