using System.Text.Json;
using System.Text.Json.Serialization;


namespace Shared.Extensions;

public static class EnumConverterExtensions
{
    /// <summary>
    /// Register a generic enum converter that:
    /// - accepts case-insensitive values and ignores '_', '-', and whitespace (e.g., OnQueue, ON_Queue, on-queue)
    /// - serializes in canonical SNAKE_UPPER (e.g., ON_QUEUE)
    /// </summary>
    public static JsonSerializerOptions AddFlexibleSnakeUpperEnums(this JsonSerializerOptions options)
    {
        options.Converters.Add(new FlexibleSnakeUpperEnumConverterFactory());

        return options;
    }
}

internal sealed class FlexibleSnakeUpperEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(FlexibleSnakeUpperEnumConverter<>).MakeGenericType(typeToConvert);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

internal sealed class FlexibleSnakeUpperEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
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
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.WriteEnumSnakeUpper());
    }
}
