using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using SharedKernel.Serialization.Json;

using Xunit;


namespace tests.Unit.SharedKernel.Serialization.Json;

public sealed class SnakeUpperEnumConverterTests
{
    private enum TestStatus
    {
        InQueue,
        AfterCallWork
    }

    private sealed class Envelope
    {
        public TestStatus Status { get; init; }
    }

    #region Read

    [Fact]
    public void Read_ReturnsEnum_WhenPropertyTokenIsValidString()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        Envelope? result = JsonSerializer.Deserialize<Envelope>("{\"Status\":\"in-queue\"}", options);

        Assert.NotNull(result);
        Assert.Equal(TestStatus.InQueue, result.Status);
    }

    [Fact]
    public void Read_ThrowsJsonException_WhenPropertyTokenIsNotString()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        JsonException ex =
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Envelope>("{\"Status\":123}", options));

        Assert.Contains("Expected string", ex.Message);
    }

    [Fact]
    public void Read_ThrowsJsonException_WhenPropertyTokenIsUnknown()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddSnakeUpperEnums();

        JsonException ex =
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Envelope>("{\"Status\":\"UNKNOWN_VALUE\"}",
                                          options));

        Assert.Contains("Unknown", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    #endregion

    #region Write

    [Fact]
    public void Write_Direct_WritesSnakeUpper()
    {
        Assembly asm = typeof(EnumJsonExtensions).Assembly;
        Type open = asm.GetType("SharedKernel.Serialization.Json.SnakeUpperEnumConverter`1", true)!;
        Type closed = open.MakeGenericType(typeof(TestStatus));
        JsonConverter<TestStatus> converter = (JsonConverter<TestStatus>)Activator.CreateInstance(closed)!;

        using MemoryStream ms = new MemoryStream();
        using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
        converter.Write(writer, TestStatus.AfterCallWork, new JsonSerializerOptions());
        writer.Flush();

        string json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        Assert.Equal("\"AFTER_CALL_WORK\"", json);
    }

    #endregion
}
