using System.Text.Json;

using FunctionApp.Http.Common;

using Xunit;


namespace tests.Unit.FunctionApp.Http.Common;

public sealed class JsonEnumParseErrorHelperTests
{
    [Fact]
    public void TryBuildMessage_RootEnumPath_ReturnsTrueWithAllowedValues()
    {
        JsonException ex = NewJsonException("$.category");

        bool ok = JsonEnumParseErrorHelper.TryBuildMessage<RootRequest>(ex, out string message);

        Assert.True(ok);
        Assert.Contains("Invalid value for 'category'.", message);
        Assert.Contains("First", message);
        Assert.Contains("Second", message);
    }

    [Fact]
    public void TryBuildMessage_NestedEnumPath_ReturnsTrue()
    {
        JsonException ex = NewJsonException("$.payload.inner.category");

        bool ok = JsonEnumParseErrorHelper.TryBuildMessage<NestedRequest>(ex, out string message);

        Assert.True(ok);
        Assert.Contains("Invalid value for 'category'.", message);
        Assert.Contains("First", message);
        Assert.Contains("Second", message);
    }

    [Fact]
    public void TryBuildMessage_IndexedNestedEnumPath_ReturnsTrue()
    {
        JsonException ex = NewJsonException("$.payload[0].inner.category");

        bool ok = JsonEnumParseErrorHelper.TryBuildMessage<NestedRequest>(ex, out string message);

        Assert.True(ok);
        Assert.Contains("Invalid value for 'category'.", message);
    }

    [Fact]
    public void TryBuildMessage_NonEnumProperty_ReturnsFalse()
    {
        JsonException ex = NewJsonException("$.name");

        bool ok = JsonEnumParseErrorHelper.TryBuildMessage<RootRequest>(ex, out string message);

        Assert.False(ok);
        Assert.Equal(string.Empty, message);
    }

    [Fact]
    public void TryBuildMessage_UnknownPath_ReturnsFalse()
    {
        JsonException ex = NewJsonException("$.notExists");

        bool ok = JsonEnumParseErrorHelper.TryBuildMessage<RootRequest>(ex, out string message);

        Assert.False(ok);
        Assert.Equal(string.Empty, message);
    }

    [Fact]
    public void TryBuildMessage_InvalidPrefix_ReturnsFalse()
    {
        JsonException ex = NewJsonException("category");

        bool ok = JsonEnumParseErrorHelper.TryBuildMessage<RootRequest>(ex, out string message);

        Assert.False(ok);
        Assert.Equal(string.Empty, message);
    }

    [Fact]
    public void TryBuildMessage_EmptyPath_ReturnsFalse()
    {
        JsonException ex = NewJsonException("");

        bool ok = JsonEnumParseErrorHelper.TryBuildMessage<RootRequest>(ex, out string message);

        Assert.False(ok);
        Assert.Equal(string.Empty, message);
    }

    #region ========== *** Private Section *** ==========

    private static JsonException NewJsonException(string path)
    {
        return new JsonException("bad value",
                                 innerException: null,
                                 path: path,
                                 lineNumber: null,
                                 bytePositionInLine: null);
    }

    private sealed class RootRequest
    {
        public TestCategory Category { get; set; }

        public string? Name { get; set; }
    }

    private sealed class NestedRequest
    {
        public Payload? Payload { get; set; }
    }

    private sealed class Payload
    {
        public Inner? Inner { get; set; }
    }

    private sealed class Inner
    {
        public TestCategory Category { get; set; }
    }

    private enum TestCategory
    {
        First,
        Second
    }

    #endregion
}
