using System.Text.Json;
using System.Text.Json.Serialization;


namespace SharedKernel.Serialization.Json;

/// <summary>
/// Factory that creates JSON converters for enum and nullable-enum types,
/// using canonical <c>SNAKE_UPPER</c> serialization and flexible enum parsing.
/// </summary>
internal sealed class SnakeUpperEnumConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Determines whether this factory can convert the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to evaluate.</param>
    /// <returns>
    /// <c>true</c> when <paramref name="typeToConvert"/> is an enum or nullable enum;
    /// otherwise <c>false</c>.
    /// </returns>
    public override bool CanConvert(Type typeToConvert)
    {
        return GetEnumType(typeToConvert) is not null;
    }

    /// <summary>
    /// Creates a converter for the specified enum or nullable-enum type.
    /// </summary>
    /// <param name="typeToConvert">The target type to convert.</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>A converter instance for the requested type.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="typeToConvert"/> is neither an enum nor a nullable enum.
    /// </exception>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type? enumType = GetEnumType(typeToConvert);
        if (enumType is null)
        {
            throw new InvalidOperationException($"Type '{typeToConvert}' is not an enum or nullable enum.");
        }

        Type converterType = typeToConvert == enumType
            ? typeof(SnakeUpperEnumConverter<>).MakeGenericType(enumType)
            : typeof(NullableSnakeUpperEnumConverter<>).MakeGenericType(enumType);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Resolves the underlying enum type for a given type.
    /// </summary>
    /// <param name="typeToConvert">A candidate enum or nullable-enum type.</param>
    /// <returns>
    /// The enum type when resolvable; otherwise <c>null</c>.
    /// </returns>
    private static Type? GetEnumType(Type typeToConvert)
    {
        if (typeToConvert.IsEnum) return typeToConvert;

        Type? underlying = Nullable.GetUnderlyingType(typeToConvert);

        return underlying?.IsEnum == true ? underlying : null;
    }

    #endregion
}
