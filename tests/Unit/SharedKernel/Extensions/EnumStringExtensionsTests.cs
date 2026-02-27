using SharedKernel.Extensions;

using Xunit;


namespace tests.Unit.SharedKernel.Extensions;

public class EnumStringExtensionsTests
{
    public enum RoutingState
    {
        InQueue,
        OffQueue
    }

    private enum CollidingTokens
    {
        InQueue,
        In_Queue
    }

    #region NormalizeEnumToken

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
        Assert.Throws<ArgumentNullException>(() => EnumStringExtensions.NormalizeEnumToken(null!));
    }

    #endregion

    #region ReadEnum

    [Theory]
    [InlineData("in_queue", RoutingState.InQueue)]
    [InlineData("IN-QUEUE", RoutingState.InQueue)]
    [InlineData(" off queue ", RoutingState.OffQueue)]
    public void ReadEnum_ReturnsExpected_WhenTokenIsKnown(string input, RoutingState expected)
    {
        RoutingState actual = input.ReadEnum<RoutingState>();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReadEnum_Throws_WhenInputIsNullOrWhitespace(string? input)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => input.ReadEnum<RoutingState>());
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ReadEnum_Throws_WhenTokenIsUnknown()
    {
        const string input = "not-a-routing-state";

        ArgumentException ex = Assert.Throws<ArgumentException>(() => input.ReadEnum<RoutingState>());
        Assert.Equal("value", ex.ParamName);
        Assert.Contains("Unknown RoutingState value", ex.Message);
    }

    [Fact]
    public void ReadEnum_Throws_WhenEnumNormalizationCollides()
    {
        TypeInitializationException ex =
            Assert.Throws<TypeInitializationException>(() => "in_queue".ReadEnum<CollidingTokens>());

        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Contains("Enum normalization collision in CollidingTokens", ex.InnerException!.Message);
    }

    #endregion
}
