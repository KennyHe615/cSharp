using SharedKernel.Extensions;

using Xunit;


namespace tests.Unit.SharedKernel.Extensions;

public sealed class StringExtensionsTests
{
    #region ToSnakeCase

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("my_value", "my_value")]
    [InlineData("my_Value", "my_value")]
    [InlineData("My__Value", "my_value")]
    [InlineData("__MyValue", "__my_value")]
    [InlineData("myValueX", "my_value_x")]
    [InlineData("HTTPServer", "http_server")]
    [InlineData("HTTP2Server", "http2_server")]
    [InlineData("東京Value", "東京_value")]
    [InlineData("Already_Snake_Case", "already_snake_case")]
    [InlineData("___", "___")]
    public void ToSnakeCase_ReturnsExpected(string? input, string? expected)
    {
        string? actual = input.ToSnakeCase();
        Assert.Equal(expected, actual);
    }

    #endregion

    #region ToSnakeUpperCase

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

    #endregion

    #region Truncate

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

    #endregion

    #region ToGuid

    [Fact]
    public void ToGuid_ReturnsGuid_WhenValid()
    {
        Guid expected = Guid.NewGuid();
        Guid? actual = expected.ToString()
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

    #endregion

    #region NormalizeToNull

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", null, null)]
    [InlineData("   ", null, null)]
    [InlineData("  abc  ", null, "abc")]
    [InlineData("  abcdef  ", 3, "abc")]
    public void NormalizeToNull_ReturnsExpected(string? input, int? maxLength, string? expected)
    {
        string? actual = input.NormalizeToNull(maxLength);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NormalizeToNull_Throws_WhenMaxLengthIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => "abc".NormalizeToNull(-1));
    }

    #endregion

    #region ToFailureReason

    [Fact]
    public void ToFailureReason_ReturnsNormalizedExceptionMessage()
    {
        Exception ex = new Exception("  Something failed while syncing.  ");

        string actual = ex.ToFailureReason(16);

        Assert.Equal("Something failed", actual);
    }

    [Fact]
    public void ToFailureReason_FallsBackToExceptionType_WhenMessageIsBlank()
    {
        Exception ex = new Exception("");

        string actual = ex.ToFailureReason();

        Assert.Equal(nameof(Exception), actual);
    }

    #endregion
}
