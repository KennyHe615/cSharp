using Application.Abstractions.Context;
using Application.Abstractions.Identity;

using Infrastructure.Configuration.Options;
using Infrastructure.Identity;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using SharedKernel.Environment;

using tests.TestSupport.Context;

using Xunit;


namespace tests.Unit.Infrastructure.Identity;

public sealed class LobCredentialProviderTests
{
    [Fact]
    public async Task PopulateAsync_WhenAccessorIsNull_ThrowsArgumentNullException()
    {
        LobCredentialProvider sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.PopulateAsync(null!));
    }

    [Fact]
    public async Task PopulateAsync_WhenLobNameMissing_ThrowsInvalidOperationException()
    {
        LobCredentialProvider sut = CreateSut();
        ILobContextAccessor accessor = new StubLobContextAccessor { LobName = "   " };

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PopulateAsync(accessor,
                                                                 CancellationToken.None));

        Assert.Contains("LobName must be set", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PopulateAsync_WhenSecretsExist_PopulatesAccessor()
    {
        Mock<ISecretProvider> secretProvider = new Mock<ISecretProvider>(MockBehavior.Strict);
        LobCredentialProvider sut = CreateSut(secretProvider);

        ILobContextAccessor accessor = new StubLobContextAccessor { LobName = "CRC" };

        secretProvider.Setup(x => x.GetSecretAsync("GenesysClientId-CRC-dev", It.IsAny<CancellationToken>()))
                      .ReturnsAsync("client-id");
        secretProvider.Setup(x => x.GetSecretAsync("GenesysClientSecret-CRC-dev", It.IsAny<CancellationToken>()))
                      .ReturnsAsync("client-secret");
        secretProvider.Setup(x => x.GetSecretAsync("LandingDbConnStr-CRC-dev", It.IsAny<CancellationToken>()))
                      .ReturnsAsync("db-conn");

        await sut.PopulateAsync(accessor, CancellationToken.None);

        Assert.Equal("client-id", accessor.GenesysClientId);
        Assert.Equal("client-secret", accessor.GenesysClientSecret);
        Assert.Equal("db-conn", accessor.DbConnectionString);
        secretProvider.VerifyAll();
    }

    [Fact]
    public async Task PopulateAsync_WhenAnyRequiredSecretMissing_ThrowsInvalidOperationException()
    {
        Mock<ISecretProvider> secretProvider = new Mock<ISecretProvider>(MockBehavior.Strict);
        LobCredentialProvider sut = CreateSut(secretProvider);

        ILobContextAccessor accessor = new StubLobContextAccessor { LobName = "CRC" };

        secretProvider.Setup(x => x.GetSecretAsync("GenesysClientId-CRC-dev", It.IsAny<CancellationToken>()))
                      .ReturnsAsync("client-id");
        secretProvider.Setup(x => x.GetSecretAsync("GenesysClientSecret-CRC-dev", It.IsAny<CancellationToken>()))
                      .ReturnsAsync("   ");
        secretProvider.Setup(x => x.GetSecretAsync("LandingDbConnStr-CRC-dev", It.IsAny<CancellationToken>()))
                      .ReturnsAsync("db-conn");

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PopulateAsync(accessor,
                                                                 CancellationToken.None));

        Assert.Contains("missing or empty", ex.Message, StringComparison.Ordinal);
        secretProvider.VerifyAll();
    }

    [Fact]
    public async Task PopulateAsync_UsesSecretNameFormat_PrefixLobEnvironmentAlias()
    {
        Mock<ISecretProvider> secretProvider = new Mock<ISecretProvider>(MockBehavior.Strict);
        LobCredentialProvider sut = CreateSut(secretProvider);

        ILobContextAccessor accessor = new StubLobContextAccessor { LobName = "NTT" };

        secretProvider.Setup(x => x.GetSecretAsync("GenesysClientId-NTT-dev", It.IsAny<CancellationToken>()))
                      .ReturnsAsync("id");
        secretProvider.Setup(x => x.GetSecretAsync("GenesysClientSecret-NTT-dev", It.IsAny<CancellationToken>()))
                      .ReturnsAsync("secret");
        secretProvider.Setup(x => x.GetSecretAsync("LandingDbConnStr-NTT-dev", It.IsAny<CancellationToken>()))
                      .ReturnsAsync("conn");

        await sut.PopulateAsync(accessor, CancellationToken.None);

        secretProvider.Verify(x => x.GetSecretAsync("GenesysClientId-NTT-dev", It.IsAny<CancellationToken>()),
                              Times.Once);
        secretProvider.Verify(x => x.GetSecretAsync("GenesysClientSecret-NTT-dev", It.IsAny<CancellationToken>()),
                              Times.Once);
        secretProvider.Verify(x => x.GetSecretAsync("LandingDbConnStr-NTT-dev", It.IsAny<CancellationToken>()),
                              Times.Once);
    }

    private static LobCredentialProvider CreateSut(Mock<ISecretProvider>? secretProviderMock = null)
    {
        Mock<ISecretProvider> secretProvider = secretProviderMock ?? new Mock<ISecretProvider>(MockBehavior.Strict);

        KeyVaultOptions options = new KeyVaultOptions
                                  {
                                      GenesysClientIdSecretPrefix = "GenesysClientId",
                                      GenesysClientSecretSecretPrefix = "GenesysClientSecret",
                                      LandingDbConnStrSecretPrefix = "LandingDbConnStr"
                                  };

        AppEnvironment appEnvironment = new AppEnvironment(AppEnvironmentKind.Development, "dev");
        Mock<ILogger<LobCredentialProvider>> logger = new Mock<ILogger<LobCredentialProvider>>();

        return new LobCredentialProvider(secretProvider.Object,
                                         Options.Create(options),
                                         appEnvironment,
                                         logger.Object);
    }
}
