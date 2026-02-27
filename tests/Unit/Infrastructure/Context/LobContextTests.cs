using Infrastructure.Context;

using SharedKernel.Lobs;

using Xunit;


namespace tests.Unit.Infrastructure.Context;

public sealed class LobContextTests
{
    [Theory]
    [InlineData("NTT", "NTT")]
    [InlineData("ntt", "NTT")]
    [InlineData("CrC", "CRC")]
    [InlineData("lcl", "LCL")]
    public void Constructor_AcceptsAllowedValues_CaseInsensitive(string input, string expected)
    {
        LobName lob = new LobName(input);

        Assert.Equal(expected, lob.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_ThrowsArgumentException_WhenValueIsNullOrWhitespace(string input)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => new LobName(input));

        Assert.Equal("value", ex.ParamName);
        Assert.Contains("LOB name is required.", ex.Message);
    }

    [Fact]
    public void Constructor_ThrowsArgumentException_WhenValueIsNull()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => new LobName(null!));

        Assert.Equal("value", ex.ParamName);
        Assert.Contains("LOB name is required.", ex.Message);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData(" NTT ")]
    [InlineData("NT")]
    public void Constructor_ThrowsArgumentOutOfRangeException_WhenValueIsNotAllowed(string input)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => new LobName(input));

        Assert.Equal("value", ex.ParamName);
        Assert.Equal(input, ex.ActualValue);
        Assert.Contains("Unsupported LOB", ex.Message);
    }

    [Fact]
    public void LobName_ReturnsValueObject_WhenPresent()
    {
        LobContextAccessor accessor = new LobContextAccessor { LobName = "ntt" };
        LobContext sut = new LobContext(accessor);

        LobName result = sut.LobName;

        Assert.Equal("NTT", result.Value);
    }

    [Fact]
    public void LobName_Throws_WhenMissing()
    {
        LobContextAccessor accessor = new LobContextAccessor();
        LobContext sut = new LobContext(accessor);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _ = sut.LobName);

        Assert.Equal("Missing LobName in context.", ex.Message);
    }

    [Fact]
    public void LobName_Throws_WhenInvalid()
    {
        LobContextAccessor accessor = new LobContextAccessor { LobName = "ABC" };
        LobContext sut = new LobContext(accessor);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => _ = sut.LobName);
        Assert.Contains("Unsupported LOB", ex.Message);
    }

    [Fact]
    public void LobName_ReturnsNtt()
    {
        LobContextAccessor accessor = new LobContextAccessor { LobName = "NTT" };
        LobContext sut = new LobContext(accessor);

        LobName result = sut.LobName;

        Assert.Equal(LobName.Ntt, result);
        Assert.Equal("NTT", result.Value);
    }

    [Fact]
    public void LobName_ReturnsCrc()
    {
        LobContextAccessor accessor = new LobContextAccessor { LobName = "CRC" };
        LobContext sut = new LobContext(accessor);

        LobName result = sut.LobName;

        Assert.Equal(LobName.Crc, result);
        Assert.Equal("CRC", result.Value);
    }

    [Fact]
    public void LobName_ReturnsLcl()
    {
        LobContextAccessor accessor = new LobContextAccessor { LobName = "LCL" };
        LobContext sut = new LobContext(accessor);

        LobName result = sut.LobName;

        Assert.Equal(LobName.Lcl, result);
        Assert.Equal("LCL", result.Value);
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

        Assert.Equal("Missing GenesysClientId for LOB `NTT`.", ex.Message);
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

        Assert.Equal("Missing GenesysClientSecret for LOB `NTT`.", ex.Message);
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

        Assert.Equal("Missing DatabaseConnectionString for LOB `NTT`.", ex.Message);
    }
}
