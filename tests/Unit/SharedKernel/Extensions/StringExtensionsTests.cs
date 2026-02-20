using SharedKernel.Extensions;

using Xunit;


namespace tests.Unit.SharedKernel.Extensions;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("my_value", "my_value")]
    [InlineData("__MyValue", "__my_value")]
    [InlineData("HTTP2Server", "http2_server")]
    [InlineData("東京Value", "東京_value")]
    [InlineData("Already_Snake_Case", "already_snake_case")]
    [InlineData("___", "___")]
    public void ToSnakeCase_ReturnsExpected(string? input, string? expected)
    {
        string? actual = input.ToSnakeCase();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("myValue", "MY_VALUE")]
    [InlineData("HTTP2Server", "HTTP2_SERVER")]
    public void ToSnakeUpperCase_ReturnsExpected(string? input, string? expected)
    {
        string? actual = input.ToSnakeUpperCase();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Truncate_ReturnsNull_WhenInputIsNull()
    {
        string? input = null;
        string? actual = input.Truncate(5);
        Assert.Null(actual);
    }

    [Theory]
    [InlineData("abc", 5, "abc")]
    [InlineData("abcdef", 3, "abc")]
    [InlineData("", 3, "")]
    [InlineData("abc", 0, "")]
    public void Truncate_ReturnsExpected(string input, int maxLength, string expected)
    {
        string? actual = input.Truncate(maxLength);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Truncate_Throws_WhenMaxLengthIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => "abc".Truncate(-1));
    }

    [Fact]
    public void ToGuid_ReturnsGuid_WhenValid()
    {
        Guid expected = Guid.NewGuid();
        Guid? actual = expected
                      .ToString()
                      .ToGuid();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void ToGuid_ReturnsNull_WhenInvalid(string? input)
    {
        Guid? actual = input.ToGuid();
        Assert.Null(actual);
    }

    [Theory]
    [InlineData("in_queue", "INQUEUE")]
    [InlineData("in-queue", "INQUEUE")]
    [InlineData(" in queue ", "INQUEUE")]
    [InlineData("In_Queue-Now", "INQUEUENOW")]
    public void NormalizeEnumToken_ReturnsExpected(string input, string expected)
    {
        string actual = input.NormalizeEnumToken();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("__--")]
    public void NormalizeEnumToken_Throws_WhenEmptyAfterNormalization(string input)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => input.NormalizeEnumToken());
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void NormalizeEnumToken_Throws_WhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => StringExtensions.NormalizeEnumToken(null!));
    }
}
