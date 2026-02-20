using Infrastructure.Context;

using Xunit;


namespace tests.Unit.Infrastructure.Context;

public sealed class LobContextTests
{
    [Fact]
    public void LobName_ReturnsValue_WhenPresent()
    {
        LobContextAccessor accessor = new LobContextAccessor { LobName = "ntt" };
        LobContext sut = new LobContext(accessor);

        string result = sut.LobName;

        Assert.Equal("ntt", result);
    }

    [Fact]
    public void LobName_Throws_WhenMissing()
    {
        LobContextAccessor accessor = new LobContextAccessor();
        LobContext sut = new LobContext(accessor);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _ = sut.LobName);

        Assert.Equal("LOB context was not initialized with a LobName.", ex.Message);
    }

    [Fact]
    public void GenesysClientId_ReturnsValue_WhenPresent()
    {
        LobContextAccessor accessor = new LobContextAccessor
                                      {
                                          LobName = "ntt",
                                          GenesysClientId = "client-id"
                                      };
        LobContext sut = new LobContext(accessor);

        string result = sut.GenesysClientId;

        Assert.Equal("client-id", result);
    }

    [Fact]
    public void GenesysClientId_Throws_WhenMissing()
    {
        LobContextAccessor accessor = new LobContextAccessor { LobName = "ntt" };
        LobContext sut = new LobContext(accessor);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _ = sut.GenesysClientId);

        Assert.Equal("Missing GenesysClientId for LOB `ntt`.", ex.Message);
    }

    [Fact]
    public void GenesysClientSecret_ReturnsValue_WhenPresent()
    {
        LobContextAccessor accessor = new LobContextAccessor
                                      {
                                          LobName = "ntt",
                                          GenesysClientSecret = "client-secret"
                                      };
        LobContext sut = new LobContext(accessor);

        string result = sut.GenesysClientSecret;

        Assert.Equal("client-secret", result);
    }

    [Fact]
    public void GenesysClientSecret_Throws_WhenMissing()
    {
        LobContextAccessor accessor = new LobContextAccessor { LobName = "ntt" };
        LobContext sut = new LobContext(accessor);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _ = sut.GenesysClientSecret);

        Assert.Equal("Missing GenesysClientSecret for LOB `ntt`.", ex.Message);
    }

    [Fact]
    public void DbConnectionString_ReturnsValue_WhenPresent()
    {
        LobContextAccessor accessor = new LobContextAccessor
                                      {
                                          LobName = "ntt",
                                          DbConnectionString =
                                              "Server=.;Database=app;Trusted_Connection=True;"
                                      };
        LobContext sut = new LobContext(accessor);

        string result = sut.DbConnectionString;

        Assert.Equal("Server=.;Database=app;Trusted_Connection=True;", result);
    }

    [Fact]
    public void DbConnectionString_Throws_WhenMissing()
    {
        LobContextAccessor accessor = new LobContextAccessor { LobName = "ntt" };
        LobContext sut = new LobContext(accessor);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _ = sut.DbConnectionString);

        Assert.Equal("Missing DatabaseConnectionString for LOB `ntt`.", ex.Message);
    }
}
