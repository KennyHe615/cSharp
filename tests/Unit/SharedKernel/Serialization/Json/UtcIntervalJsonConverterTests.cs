using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

using SharedKernel.Serialization.Json;
using SharedKernel.Time;

using Xunit;


namespace tests.Unit.SharedKernel.Serialization.Json;

public sealed class UtcIntervalJsonConverterTests
{
    #region ========== *** Read *** ==========

    [Fact]
    public void Read_WithValidString_ReturnsInterval()
    {
        UtcIntervalJsonConverter converter = new UtcIntervalJsonConverter();
        Utf8JsonReader reader = CreateReader("\"2025-01-01T00:00Z/2025-01-01T01:00Z\"");

        UtcInterval result = converter.Read(ref reader, typeof(UtcInterval), new JsonSerializerOptions());

        Assert.Equal(new DateTimeOffset(2025,
                                        1,
                                        1,
                                        0,
                                        0,
                                        0,
                                        TimeSpan.Zero),
                     result.Start);
        Assert.Equal(new DateTimeOffset(2025,
                                        1,
                                        1,
                                        1,
                                        0,
                                        0,
                                        TimeSpan.Zero),
                     result.End);
    }

    [Fact]
    public void Read_WithNonStringToken_ThrowsJsonException()
    {
        JsonException ex = Assert.Throws<JsonException>(() => ReadFromJson("123"));

        Assert.Contains("Interval must be a JSON string.", ex.Message);
    }

    [Fact]
    public void Read_WithInvalidFormat_ThrowsJsonException()
    {
        JsonException ex = Assert.Throws<JsonException>(() => ReadFromJson("\"invalid\""));

        Assert.Contains("Invalid interval format", ex.Message);
    }

    #endregion

    #region ========== *** Write *** ==========

    [Fact]
    public void Write_WritesNormalizedString()
    {
        UtcIntervalJsonConverter converter = new UtcIntervalJsonConverter();
        UtcInterval value = new UtcInterval(new DateTimeOffset(2025,
                                                               1,
                                                               1,
                                                               0,
                                                               0,
                                                               30,
                                                               TimeSpan.Zero),
                                            new DateTimeOffset(2025,
                                                               1,
                                                               1,
                                                               1,
                                                               0,
                                                               45,
                                                               TimeSpan.Zero));

        using MemoryStream ms = new MemoryStream();
        using Utf8JsonWriter writer = new Utf8JsonWriter(ms);

        converter.Write(writer, value, new JsonSerializerOptions());
        writer.Flush();

        string json = Encoding.UTF8.GetString(ms.ToArray());
        Assert.Equal("\"2025-01-01T00:00Z/2025-01-01T01:00Z\"", json);
    }

    #endregion

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private static Utf8JsonReader CreateReader(string json)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(json);
        Utf8JsonReader reader = new Utf8JsonReader(utf8);
        reader.Read();

        return reader;
    }

    [ExcludeFromCodeCoverage]
    private static UtcInterval ReadFromJson(string json)
    {
        UtcIntervalJsonConverter converter = new UtcIntervalJsonConverter();
        Utf8JsonReader reader = CreateReader(json);

        return converter.Read(ref reader, typeof(UtcInterval), new JsonSerializerOptions());
    }

    #endregion
}
