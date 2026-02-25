using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using SharedKernel.Serialization.Json;

using Xunit;


namespace tests.Unit.SharedKernel.Serialization.Json;

public sealed class NullableSnakeUpperEnumConverterTests
{
    public enum TestStatus
    {
        InQueue,
        OnBreak,
        AfterCallWork
    }

    private sealed class Envelope
    {
        public TestStatus? Status { get; init; }
    }

    #region Read

    [Fact]
    public void Read_ReturnsNull_WhenPropertyTokenIsNull()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        Envelope? result = JsonSerializer.Deserialize<Envelope>("{\"Status\":null}", options);

        Assert.NotNull(result);
        Assert.Null(result.Status);
    }

    [Theory]
    [InlineData("{\"Status\":\"IN_QUEUE\"}", TestStatus.InQueue)]
    [InlineData("{\"Status\":\"in-queue\"}", TestStatus.InQueue)]
    [InlineData("{\"Status\":\"on break\"}", TestStatus.OnBreak)]
    public void Read_ReturnsEnum_WhenPropertyTokenIsValidString(string json, TestStatus expected)
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        Envelope? result = JsonSerializer.Deserialize<Envelope>(json, options);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public void Read_ThrowsJsonException_WhenPropertyTokenIsUnknown()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        JsonException ex =
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Envelope>("{\"Status\":\"UNKNOWN_VALUE\"}",
                                          options));

        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void Read_ThrowsJsonException_WhenPropertyTokenIsNotStringOrNull()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        JsonException ex =
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Envelope>("{\"Status\":123}", options));

        Assert.Contains("Expected string or null", ex.Message);
    }

    [Fact]
    public void Read_ReturnsNull_WhenTokenIsNull_Direct()
    {
        JsonConverter<TestStatus?> converter = CreateConverter();
        Utf8JsonReader reader = CreateReader("null");

        TestStatus? result = converter.Read(ref reader, typeof(TestStatus?), new JsonSerializerOptions());

        Assert.Null(result);
    }

    [Fact]
    public void Read_ReturnsEnum_WhenTokenIsString_Direct()
    {
        JsonConverter<TestStatus?> converter = CreateConverter();
        Utf8JsonReader reader = CreateReader("\"in-queue\"");

        TestStatus? result = converter.Read(ref reader, typeof(TestStatus?), new JsonSerializerOptions());

        Assert.Equal(TestStatus.InQueue, result);
    }

    [Fact]
    public void Read_ThrowsJsonException_WhenTokenIsNotStringOrNull_Direct()
    {
        JsonConverter<TestStatus?> converter = CreateConverter();
        Utf8JsonReader reader = CreateReader("123");

        JsonException ex;
        try
        {
            _ = converter.Read(ref reader, typeof(TestStatus?), new JsonSerializerOptions());

            throw new Xunit.Sdk.XunitException("Expected JsonException was not thrown.");
        }
        catch (JsonException caught)
        {
            ex = caught;
        }

        Assert.Contains("Expected string or null", ex.Message);
    }

    [Fact]
    public void Read_ThrowsJsonException_WhenTokenIsUnknown_Direct()
    {
        JsonConverter<TestStatus?> converter = CreateConverter();
        Utf8JsonReader reader = CreateReader("\"UNKNOWN\"");

        JsonException ex;
        try
        {
            _ = converter.Read(ref reader, typeof(TestStatus?), new JsonSerializerOptions());

            throw new Xunit.Sdk.XunitException("Expected JsonException was not thrown.");
        }
        catch (JsonException caught)
        {
            ex = caught;
        }

        Assert.NotNull(ex.InnerException);
    }

    #endregion

    #region Write

    [Fact]
    public void Write_WritesNull_WhenPropertyValueIsNull()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();
        Envelope value = new Envelope { Status = null };

        string json = JsonSerializer.Serialize(value, options);

        Assert.Contains("\"Status\":null", json);
    }

    [Fact]
    public void Write_WritesSnakeUpper_WhenPropertyValueHasValue()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();
        Envelope value = new Envelope { Status = TestStatus.AfterCallWork };

        string json = JsonSerializer.Serialize(value, options);

        Assert.Contains("\"Status\":\"AFTER_CALL_WORK\"", json);
    }

    [Fact]
    public void Write_WritesNull_WhenValueIsNull_Direct()
    {
        JsonConverter<TestStatus?> converter = CreateConverter();

        string json = WriteWithConverter(converter, null);

        Assert.Equal("null", json);
    }

    [Fact]
    public void Write_WritesSnakeUpper_WhenValueHasValue_Direct()
    {
        JsonConverter<TestStatus?> converter = CreateConverter();

        string json = WriteWithConverter(converter, TestStatus.AfterCallWork);

        Assert.Equal("\"AFTER_CALL_WORK\"", json);
    }

    #endregion

    #region ========== *** Private Methods *** ==========

    private static JsonConverter<TestStatus?> CreateConverter()
    {
        Assembly assembly = typeof(EnumJsonExtensions).Assembly;
        Type openType = assembly.GetType("SharedKernel.Serialization.Json.NullableSnakeUpperEnumConverter`1", true)!;
        Type closedType = openType.MakeGenericType(typeof(TestStatus));

        return (JsonConverter<TestStatus?>)Activator.CreateInstance(closedType)!;
    }

    private static Utf8JsonReader CreateReader(string json)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(json);
        Utf8JsonReader reader = new Utf8JsonReader(utf8);
        reader.Read();// move to first token

        return reader;
    }

    private static string WriteWithConverter(JsonConverter<TestStatus?> converter, TestStatus? value)
    {
        using MemoryStream ms = new MemoryStream();
        using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
        converter.Write(writer, value, new JsonSerializerOptions());
        writer.Flush();

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    #endregion
}
