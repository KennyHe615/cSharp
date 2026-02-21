using System.Text.Json;
using System.Text.Json.Serialization;

using SharedKernel.Extensions;


namespace SharedKernel.Serialization.Json;

/// <summary>
/// JSON converter for non-nullable enum values that parses normalized enum strings
/// and serializes values in canonical <c>SNAKE_UPPER</c> format.
/// </summary>
/// <typeparam name="TEnum">The enum type being converted.</typeparam>
internal sealed class SnakeUpperEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>
    /// Reads an enum value from a JSON string token.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the token to convert.</param>
    /// <param name="typeToConvert">The target type to convert.</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>The parsed enum value.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the token is not a string, is null, or cannot be mapped to a valid enum member.
    /// </exception>
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string for {typeof(TEnum).Name}.");
        }

        string? raw = reader.GetString();
        if (raw is null)
        {
            throw new JsonException($"Empty value for {typeof(TEnum).Name}.");
        }

        try
        {
            return raw.ReadEnum<TEnum>();
        }
        catch (ArgumentException ex)
        {
            throw new JsonException(ex.Message, ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new JsonException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Writes an enum value as a canonical <c>SNAKE_UPPER</c> JSON string.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The enum value to write.</param>
    /// <param name="options">Serializer options.</param>
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.WriteEnumSnakeUpper());
    }
}
