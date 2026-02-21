using System.Text.Json;

using SharedKernel.Serialization.Json;

using Xunit;


namespace tests.Unit.SharedKernel.Serialization.Json;

public sealed class EnumJsonExtensionsTests
{
    [Fact]
    public void AddSnakeUpperEnums_Throws_WhenOptionsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => EnumJsonExtensions.AddSnakeUpperEnums(null!));
    }

    [Fact]
    public void AddSnakeUpperEnums_IsIdempotent()
    {
        JsonSerializerOptions options = new JsonSerializerOptions();

        options.AddSnakeUpperEnums();
        int firstCount = options.Converters.Count;

        options.AddSnakeUpperEnums();
        int secondCount = options.Converters.Count;

        Assert.Equal(1, firstCount);
        Assert.Equal(firstCount, secondCount);
    }

    [Fact]
    public void Serialize_Enum_WritesSnakeUpper()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        string json = JsonSerializer.Serialize(TestStatus.InQueue, options);

        Assert.Equal("\"IN_QUEUE\"", json);
    }

    [Theory]
    [InlineData("\"IN_QUEUE\"")]
    [InlineData("\"in_queue\"")]
    [InlineData("\"in-queue\"")]
    [InlineData("\"in queue\"")]
    [InlineData("\"InQueue\"")]
    public void Deserialize_Enum_ReadsFlexibleTokens(string json)
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        TestStatus result = JsonSerializer.Deserialize<TestStatus>(json, options);

        Assert.Equal(TestStatus.InQueue, result);
    }

    [Fact]
    public void Deserialize_Enum_ThrowsJsonException_ForNonStringToken()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TestStatus>("123", options));

        Assert.Contains("Expected string", ex.Message);
    }

    [Fact]
    public void Deserialize_Enum_ThrowsJsonException_ForUnknownValue()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        JsonException ex =
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TestStatus>("\"UNKNOWN_VALUE\"", options));

        Assert.Contains("Unknown", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void Deserialize_NullableEnum_ReadsNull()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        TestStatus? result = JsonSerializer.Deserialize<TestStatus?>("null", options);

        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_NullableEnum_ReadsValue()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        TestStatus? result = JsonSerializer.Deserialize<TestStatus?>("\"ON_BREAK\"", options);

        Assert.Equal(TestStatus.OnBreak, result);
    }

    [Fact]
    public void Serialize_NullableEnum_WritesNull_WhenNull()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        TestStatus? value = null;
        string json = JsonSerializer.Serialize(value, options);

        Assert.Equal("null", json);
    }

    [Fact]
    public void Serialize_NullableEnum_WritesSnakeUpper_WhenHasValue()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        TestStatus? value = TestStatus.AfterCallWork;
        string json = JsonSerializer.Serialize(value, options);

        Assert.Equal("\"AFTER_CALL_WORK\"", json);
    }

    [Fact]
    public void Converter_AppliesToNullableEnumProperty()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        Envelope? result = JsonSerializer.Deserialize<Envelope>("{\"Status\":\"on-break\"}", options);

        Assert.NotNull(result);
        Assert.Equal(TestStatus.OnBreak, result.Status);
    }

    [Fact]
    public void Converter_WritesNullableEnumProperty_AsSnakeUpper()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();
        Envelope envelope = new Envelope { Status = TestStatus.AfterCallWork };

        string json = JsonSerializer.Serialize(envelope, options);

        Assert.Contains("\"Status\":\"AFTER_CALL_WORK\"", json);
    }

    private enum TestStatus
    {
        InQueue,
        OnBreak,
        AfterCallWork
    }

    private sealed class Envelope
    {
        public TestStatus? Status { get; set; }
    }
}
