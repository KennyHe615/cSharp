using Infrastructure.Context;

using Xunit;


namespace tests.Unit.Infrastructure.Context;

public sealed class LobContextAccessorTests
{
    [Fact]
    public void Properties_CanBeSet_AndReadBack()
    {
        LobContextAccessor sut = new LobContextAccessor
                                 {
                                     LobName = "ntt",
                                     GenesysClientId = "client-id",
                                     GenesysClientSecret = "client-secret",
                                     DbConnectionString = "connection-string"
                                 };

        Assert.Equal("ntt", sut.LobName);
        Assert.Equal("client-id", sut.GenesysClientId);
        Assert.Equal("client-secret", sut.GenesysClientSecret);
        Assert.Equal("connection-string", sut.DbConnectionString);
    }
}
