using System.Text.Json;

using Application.Abstractions.Identity;

using Infrastructure.Configuration.Options;
using Infrastructure.ExternalApis.Providers.Genesys.Auth;
using Infrastructure.ExternalApis.Providers.Genesys.Auth.Contracts;
using Infrastructure.Identity;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using SharedKernel.Environment;

using Xunit;


namespace tests.Unit.Infrastructure.ExternalApis.Providers.Genesys.Auth;

public sealed class GenesysTokenStoreTests
{
    #region ========== *** TryGetValidAsync *** ==========

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TryGetValidAsync_Throws_WhenLobKeyInvalid(string? lobKey)
    {
        TestContext ctx = CreateContext();

        await Assert.ThrowsAsync<ArgumentException>(() => ctx.Sut.TryGetValidAsync(lobKey!, CancellationToken.None));
    }

    [Fact]
    public async Task TryGetValidAsync_ReturnsCachedToken_WithoutCallingSecretProvider()
    {
        TestContext ctx = CreateContext();
        GenesysTokenCacheEntry cached = NewValidEntry("cached-token");

        ctx.Cache.Set(BuildCacheKey("dev", "CRC"), cached, cached.ExpiresAtUtc);

        GenesysTokenCacheEntry? result = await ctx.Sut.TryGetValidAsync("CRC", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("cached-token", result.AccessToken);

        ctx.SecretProvider.Verify(x => x.TryGetSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                                  Times.Never);
    }

    [Fact]
    public async Task TryGetValidAsync_FetchesFromSecretProvider_Caches_AndReusesCache()
    {
        TestContext ctx = CreateContext();
        GenesysTokenCacheEntry kvEntry = NewValidEntry("kv-token");
        string payload = JsonSerializer.Serialize(kvEntry);

        ctx.SecretProvider.Setup(x => x.TryGetSecretAsync("GenesysToken-dev-CRC", It.IsAny<CancellationToken>()))
           .ReturnsAsync(payload);

        GenesysTokenCacheEntry? first = await ctx.Sut.TryGetValidAsync("CRC", CancellationToken.None);
        GenesysTokenCacheEntry? second = await ctx.Sut.TryGetValidAsync("CRC", CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal("kv-token", first.AccessToken);
        Assert.Equal("kv-token", second.AccessToken);

        ctx.SecretProvider.Verify(x => x.TryGetSecretAsync("GenesysToken-dev-CRC", It.IsAny<CancellationToken>()),
                                  Times.Once);
    }

    [Fact]
    public async Task TryGetValidAsync_ReturnsNull_WhenSecretProviderPayloadIsInvalidJson()
    {
        TestContext ctx = CreateContext();

        ctx.SecretProvider.Setup(x => x.TryGetSecretAsync("GenesysToken-dev-CRC", It.IsAny<CancellationToken>()))
           .ReturnsAsync("{ invalid-json");

        GenesysTokenCacheEntry? result = await ctx.Sut.TryGetValidAsync("CRC", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetValidAsync_ReturnsNull_WhenSecretProviderThrowsKeyVaultSecretException()
    {
        TestContext ctx = CreateContext();

        ctx.SecretProvider.Setup(x => x.TryGetSecretAsync("GenesysToken-dev-CRC", It.IsAny<CancellationToken>()))
           .ThrowsAsync(new KeyVaultSecretException("kv unavailable"));

        GenesysTokenCacheEntry? result = await ctx.Sut.TryGetValidAsync("CRC", CancellationToken.None);

        Assert.Null(result);
    }

    #endregion

    #region ========== *** UpsertAsync *** ==========

    [Fact]
    public async Task UpsertAsync_WritesCache_AndPersistsToSecretProvider()
    {
        TestContext ctx = CreateContext();
        GenesysTokenCacheEntry entry = NewValidEntry("upsert-token");

        string? persistedPayload = null;

        ctx.SecretProvider.Setup(x => x.UpsertSecretAsync("GenesysToken-dev-CRC",
                                                          It.IsAny<string>(),
                                                          It.IsAny<CancellationToken>()))
           .Callback<string, string, CancellationToken>((_, payload, _) => persistedPayload = payload)
           .Returns(Task.CompletedTask);

        await ctx.Sut.UpsertAsync("CRC", entry, CancellationToken.None);

        Assert.True(ctx.Cache.TryGetValue(BuildCacheKey("dev", "CRC"), out GenesysTokenCacheEntry? cached));
        Assert.NotNull(cached);
        Assert.Equal("upsert-token", cached.AccessToken);

        Assert.False(string.IsNullOrWhiteSpace(persistedPayload));
        GenesysTokenCacheEntry? deserialized =
            JsonSerializer.Deserialize<GenesysTokenCacheEntry>(persistedPayload!,
                                                               new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(deserialized);
        Assert.Equal("upsert-token", deserialized.AccessToken);

        ctx.SecretProvider.Verify(x => x.UpsertSecretAsync("GenesysToken-dev-CRC",
                                                           It.IsAny<string>(),
                                                           It.IsAny<CancellationToken>()),
                                  Times.Once);
    }

    #endregion

    #region ========== *** RemoveAsync *** ==========

    [Fact]
    public async Task RemoveAsync_RemovesCache_AndBestEffortDeletesSecret()
    {
        TestContext ctx = CreateContext();
        GenesysTokenCacheEntry cached = NewValidEntry("token");
        string cacheKey = BuildCacheKey("dev", "CRC");
        ctx.Cache.Set(cacheKey, cached, cached.ExpiresAtUtc);

        ctx.SecretProvider.Setup(x => x.DeleteSecretAsync("GenesysToken-dev-CRC", It.IsAny<CancellationToken>()))
           .ThrowsAsync(new KeyVaultSecretException("delete failed"));

        await ctx.Sut.RemoveAsync("CRC", CancellationToken.None);

        Assert.False(ctx.Cache.TryGetValue(cacheKey, out _));

        ctx.SecretProvider.Verify(x => x.DeleteSecretAsync("GenesysToken-dev-CRC", It.IsAny<CancellationToken>()),
                                  Times.Once);
    }

    #endregion

    #region ========== *** Private Section *** ==========

    private static TestContext CreateContext()
    {
        MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

        Mock<ISecretProvider> secretProvider = new Mock<ISecretProvider>(MockBehavior.Strict);

        KeyVaultOptions options = new KeyVaultOptions
                                  {
                                      Uri = "https://unit-test.vault.azure.net/",
                                      GenesysClientIdSecretPrefix = "GenesysClientId",
                                      GenesysClientSecretSecretPrefix = "GenesysClientSecret",
                                      GenesysTokenSecretPrefix = "GenesysToken",
                                      LandingDbConnStrSecretPrefix = "LandingDbConnStr",
                                      CacheDurationMinutes = 60
                                  };

        AppEnvironment appEnvironment = new AppEnvironment(AppEnvironmentKind.Development, "dev");

        Mock<ILogger<GenesysTokenStore>> logger = new Mock<ILogger<GenesysTokenStore>>(MockBehavior.Loose);

        GenesysTokenStore sut = new GenesysTokenStore(cache,
                                                      secretProvider.Object,
                                                      Options.Create(options),
                                                      appEnvironment,
                                                      logger.Object);

        return new TestContext(sut, cache, secretProvider);
    }

    private static GenesysTokenCacheEntry NewValidEntry(string token)
    {
        return new GenesysTokenCacheEntry(token, DateTimeOffset.UtcNow.AddMinutes(30));
    }

    private static string BuildCacheKey(string envAlias, string lobKey)
    {
        return $"genesys:oauth:{envAlias}:{lobKey}";
    }

    private sealed record TestContext(GenesysTokenStore Sut,
                                      IMemoryCache Cache,
                                      Mock<ISecretProvider> SecretProvider);

    #endregion
}
