using SharedKernel.Lobs;

using Xunit;


namespace tests.Unit.SharedKernel.Lobs;

public sealed class LobNameTests
{
    #region Constructor

    [Theory]
    [InlineData("NTT", "NTT")]
    [InlineData("ntt", "NTT")]
    [InlineData("CrC", "CRC")]
    [InlineData("lcl", "LCL")]
    public void Constructor_AcceptsAllowedValues_CaseInsensitive(string input, string expected)
    {
        LobName actual = new LobName(input);

        Assert.Equal(expected, actual.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_ThrowsArgumentException_WhenValueIsWhitespace(string input)
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
    [InlineData("NT")]
    [InlineData(" NTT ")]
    public void Constructor_ThrowsArgumentOutOfRangeException_WhenUnsupported(string input)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => new LobName(input));

        Assert.Equal("value", ex.ParamName);
        Assert.Equal(input, ex.ActualValue);
        Assert.Contains("Unsupported LOB", ex.Message);
    }

    #endregion

    [Fact]
    public void StaticProperties_ReturnExpectedValues()
    {
        Assert.Equal("NTT", LobName.Ntt.Value);
        Assert.Equal("CRC", LobName.Crc.Value);
        Assert.Equal("LCL", LobName.Lcl.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        LobName lob = new LobName("ntt");

        string actual = lob.ToString();

        Assert.Equal("NTT", actual);
    }

    [Fact]
    public void ImplicitConversionToString_ReturnsValue()
    {
        LobName lob = new LobName("crc");

        string actual = lob.Value;

        Assert.Equal("CRC", actual);
    }

    [Fact]
    public void Equality_IsValueBased_AfterNormalization()
    {
        LobName left = new LobName("ntt");
        LobName right = new LobName("NTT");

        Assert.Equal(left, right);
    }
}
