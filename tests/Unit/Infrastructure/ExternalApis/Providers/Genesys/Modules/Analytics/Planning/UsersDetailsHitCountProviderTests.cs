using System.Diagnostics.CodeAnalysis;

using Application.Abstractions.External;
using Application.Contracts.ExternalApis.Genesys.UsersDetails;

using Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.Planning;

using Xunit;


namespace tests.Unit.Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.Planning;

public sealed class UsersDetailsHitCountProviderTests
{
    [Fact]
    public void Ctor_NullClient_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new UsersDetailsHitCountProvider(null!));
    }

    [Fact]
    public async Task GetHitCountAsync_EndNotGreaterThanStart_ShouldThrowArgumentException()
    {
        FakeUsersDetailsClient client = new FakeUsersDetailsClient();
        UsersDetailsHitCountProvider sut = new UsersDetailsHitCountProvider(client);

        DateTimeOffset start = new DateTimeOffset(2026,
                                                  3,
                                                  17,
                                                  10,
                                                  0,
                                                  0,
                                                  TimeSpan.Zero);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.GetHitCountAsync(start, start));
        await Assert.ThrowsAsync<ArgumentException>(() => sut.GetHitCountAsync(start, start.AddMinutes(-1)));
    }

    [Fact]
    public async Task GetHitCountAsync_ValidInterval_ShouldDelegateAndReturnValue()
    {
        FakeUsersDetailsClient client = new FakeUsersDetailsClient
                                        {
                                            HitCount = 1234
                                        };

        UsersDetailsHitCountProvider sut = new UsersDetailsHitCountProvider(client);

        DateTimeOffset start = new DateTimeOffset(2026,
                                                  3,
                                                  17,
                                                  10,
                                                  0,
                                                  0,
                                                  TimeSpan.Zero);
        DateTimeOffset end = new DateTimeOffset(2026,
                                                3,
                                                17,
                                                11,
                                                0,
                                                0,
                                                TimeSpan.Zero);

        int result = await sut.GetHitCountAsync(start, end);

        Assert.Equal(1234, result);
        Assert.Equal(start, client.LastStart);
        Assert.Equal(end, client.LastEnd);
        Assert.Equal(1, client.GetHitCountCallCount);
    }

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private sealed class FakeUsersDetailsClient : IAnalyticsUsersDetailsClient
    {
        public int HitCount { get; set; }

        public DateTimeOffset? LastStart { get; private set; }

        public DateTimeOffset? LastEnd { get; private set; }

        public int GetHitCountCallCount { get; private set; }

        public Task<UsersDetailsRawContract> GetUsersDetailsAsync(string intervalIso8601,
                                                                  int pageNumber,
                                                                  int? pageSize = null,
                                                                  CancellationToken ct = default)
        {
            return Task.FromResult(new UsersDetailsRawContract());
        }

        public Task<int> GetHitCountAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default)
        {
            LastStart = start;
            LastEnd = end;
            GetHitCountCallCount++;

            return Task.FromResult(HitCount);
        }
    }

    #endregion
}
