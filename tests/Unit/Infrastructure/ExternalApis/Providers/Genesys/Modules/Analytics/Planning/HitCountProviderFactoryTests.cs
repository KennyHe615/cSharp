using System.Diagnostics.CodeAnalysis;

using Application.Abstractions.External;
using Application.Abstractions.Planning;
using Application.Contracts.ExternalApis.Genesys.UsersDetails;
using Application.Enums;

using Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.Planning;

using Microsoft.Extensions.DependencyInjection;

using Xunit;


namespace tests.Unit.Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.Planning;

/// <summary>
/// Unit tests for <see cref="HitCountProviderFactory"/>.
/// </summary>
public sealed class HitCountProviderFactoryTests
{
    /// <summary>
    /// Verifies that UsersDetails resolves the UsersDetails hit-count provider.
    /// </summary>
    [Fact]
    public void Create_UsersDetails_ReturnsUsersDetailsHitCountProvider()
    {
        using ServiceProvider serviceProvider = BuildServiceProvider();
        HitCountProviderFactory sut = new HitCountProviderFactory(serviceProvider);

        IHitCountProvider provider = sut.Create(SyncAnalyticsCategory.UsersDetails);

        Assert.IsType<UsersDetailsHitCountProvider>(provider);
    }

    /// <summary>
    /// Verifies that ConversationsDetails resolves the ConversationsDetails hit-count provider.
    /// </summary>
    [Fact]
    public void Create_ConversationsDetails_ReturnsConversationsDetailsHitCountProvider()
    {
        using ServiceProvider serviceProvider = BuildServiceProvider();
        HitCountProviderFactory sut = new HitCountProviderFactory(serviceProvider);

        IHitCountProvider provider = sut.Create(SyncAnalyticsCategory.ConversationsDetails);

        Assert.IsType<ConversationsDetailsHitCountProvider>(provider);
    }

    /// <summary>
    /// Verifies that unsupported analytics categories throw a clear not-supported exception.
    /// </summary>
    [Fact]
    public void Create_UnsupportedCategory_ThrowsNotSupportedException()
    {
        using ServiceProvider serviceProvider = BuildServiceProvider();
        HitCountProviderFactory sut = new HitCountProviderFactory(serviceProvider);

        NotSupportedException ex =
                Assert.Throws<NotSupportedException>(() => sut.Create(SyncAnalyticsCategory.ConversationsAggregates));

        Assert.Contains("does not support analytics hit-count planning",
                        ex.Message,
                        StringComparison.OrdinalIgnoreCase);
    }

    #region ========== *** Private Section *** ==========

    private static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = [];

        services.AddSingleton<IAnalyticsUsersDetailsClient, FakeAnalyticsUsersDetailsClient>();
        services.AddTransient<UsersDetailsHitCountProvider>();
        services.AddTransient<ConversationsDetailsHitCountProvider>();

        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeAnalyticsUsersDetailsClient : IAnalyticsUsersDetailsClient
    {
        public Task<UsersDetailsRawContract> GetUsersDetailsAsync(string intervalIso8601,
                                                                  int pageNumber,
                                                                  int? pageSize = null,
                                                                  CancellationToken ct = default)
        {
            return Task.FromResult(new UsersDetailsRawContract());
        }

        public Task<int> GetHitCountAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default)
        {
            return Task.FromResult(0);
        }
    }

    #endregion
}
