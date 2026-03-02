using System.Text.Json;
using System.Text.Json.Serialization;

using SharedKernel.Time;


namespace SharedKernel.Serialization.Json;

/// <summary>
/// JSON converter for <see cref="UtcInterval"/> values.
/// </summary>
public sealed class UtcIntervalJsonConverter : JsonConverter<UtcInterval>
{
    /// <summary>
    /// Reads a JSON string and converts it to a <see cref="UtcInterval"/>.
    /// </summary>
    /// <param name="reader">UTF-8 JSON reader.</param>
    /// <param name="typeToConvert">Target type to convert.</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>Parsed <see cref="UtcInterval"/> instance.</returns>
    /// <exception cref="JsonException">
    /// Thrown when JSON token is not a string or interval format is invalid.
    /// </exception>
    public override UtcInterval Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Interval must be a JSON string.");
        }

        string? raw = reader.GetString();
        if (!UtcInterval.TryParse(raw, out UtcInterval interval))
        {
            throw new
                JsonException("Invalid interval format. Expected UTC interval: yyyy-MM-ddTHH:mmZ/yyyy-MM-ddTHH:mmZ.");
        }

        return interval;
    }

    /// <summary>
    /// Writes a <see cref="UtcInterval"/> as a normalized UTC interval string.
    /// </summary>
    /// <param name="writer">UTF-8 JSON writer.</param>
    /// <param name="value">Interval value to serialize.</param>
    /// <param name="options">Serializer options.</param>
    public override void Write(Utf8JsonWriter writer, UtcInterval value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
