using System.Text.Json;
using System.Text.Json.Serialization;

using SharedKernel.Extensions;


namespace SharedKernel.Serialization.Json;

/// <summary>
/// JSON converter for nullable enum values that:
/// <list type="bullet">
/// <item><description>accepts <c>null</c> JSON tokens,</description></item>
/// <item><description>parses string tokens using normalized enum matching,</description></item>
/// <item><description>writes enum values in canonical <c>SNAKE_UPPER</c> format.</description></item>
/// </list>
/// </summary>
/// <typeparam name="TEnum">The enum type being converted.</typeparam>
internal sealed class NullableSnakeUpperEnumConverter<TEnum> : JsonConverter<TEnum?>
    where TEnum : struct, Enum
{
    /// <summary>
    /// Reads a nullable enum value from JSON.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the token to convert.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>
    /// The parsed enum value when token is a string; otherwise <c>null</c> when token is JSON null.
    /// </returns>
    /// <exception cref="JsonException">
    /// Thrown when token type is not string/null or the string value cannot be converted to <typeparamref name="TEnum"/>.
    /// </exception>
    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string or null for {typeof(TEnum).Name}.");
        }

        string? raw = reader.GetString();

        if (raw is null) return null;

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
    /// Writes a nullable enum value to JSON.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The nullable enum value.</param>
    /// <param name="options">Serializer options.</param>
    /// <remarks>
    /// Writes JSON null when <paramref name="value"/> is null; otherwise writes canonical <c>SNAKE_UPPER</c>.
    /// </remarks>
    public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();

            return;
        }

        writer.WriteStringValue(value.Value.WriteEnumSnakeUpper());
    }
}
