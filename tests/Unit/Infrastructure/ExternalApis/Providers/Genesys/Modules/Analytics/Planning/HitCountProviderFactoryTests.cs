using System.Diagnostics.CodeAnalysis;

using Application.Abstractions.External;
using Application.Abstractions.Planning;
using Application.Contracts.ExternalApis.Genesys.UsersDetails;
using Application.Enums;

using Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.Planning;

using Microsoft.Extensions.DependencyInjection;

using Xunit;


namespace tests.Unit.Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.Planning;

public sealed class HitCountProviderFactoryTests
{
    [Fact]
    public void Create_UsersDetails_ReturnsUsersDetailsHitCountProvider()
    {
        IServiceProvider serviceProvider = BuildServiceProvider();
        HitCountProviderFactory sut = new HitCountProviderFactory(serviceProvider);

        IHitCountProvider provider = sut.Create(SyncCategory.UsersDetails);

        Assert.IsType<UsersDetailsHitCountProvider>(provider);
    }

    [Fact]
    public void Create_ConversationsDetails_ReturnsConversationsDetailsHitCountProvider()
    {
        IServiceProvider serviceProvider = BuildServiceProvider();
        HitCountProviderFactory sut = new HitCountProviderFactory(serviceProvider);

        IHitCountProvider provider = sut.Create(SyncCategory.ConversationsDetails);

        Assert.IsType<ConversationsDetailsHitCountProvider>(provider);
    }

    [Fact]
    public void Create_UnsupportedCategory_ThrowsNotSupportedException()
    {
        IServiceProvider serviceProvider = BuildServiceProvider();
        HitCountProviderFactory sut = new HitCountProviderFactory(serviceProvider);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => sut.Create(SyncCategory.Queue));

        Assert.Contains("does not support analytics hit-count planning",
                        ex.Message,
                        StringComparison.OrdinalIgnoreCase);
    }

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
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
