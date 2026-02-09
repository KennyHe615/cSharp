using AutoMapper;

using Shared.Extensions;


namespace Infrastructure.Persistence.Mappers.Shared;

/// <summary>
/// AutoMapper type converter that converts string values to enum types.
/// </summary>
/// <typeparam name="TEnum">The target enum type to convert to. Must be a struct and an Enum.</typeparam>
/// <remarks>
/// This converter uses the <see cref="EnumStringExtensions.ReadEnum{TEnum}"/> extension method
/// to perform the conversion, which handles various string formats including snake_case and PascalCase.
/// </remarks>
public sealed class StringToEnumConverter<TEnum> : ITypeConverter<string, TEnum> where TEnum : struct, Enum
{
    /// <summary>
    /// Converts a string value to the specified enum type.
    /// </summary>
    /// <param name="source">The source string to convert.</param>
    /// <param name="destination">The destination enum value (not used in conversion).</param>
    /// <param name="context">The resolution context provided by AutoMapper.</param>
    /// <returns>The converted enum value of type <typeparamref name="TEnum"/>.</returns>
    public TEnum Convert(string source, TEnum destination, ResolutionContext context)
    {
        return source.ReadEnum<TEnum>();
    }
}

/// <summary>
/// AutoMapper type converter that converts enum values to uppercase snake_case string representation.
/// </summary>
/// <typeparam name="TEnum">The source enum type to convert from. Must be a struct and an Enum.</typeparam>
/// <remarks>
/// This converter uses the <see cref="EnumStringExtensions.WriteEnumSnakeUpper{TEnum}"/> extension method
/// to format the enum value as an uppercase snake_case string (e.g., "ROUTING_STATUS").
/// </remarks>
public sealed class EnumToStringSnakeUpperConverter<TEnum> : ITypeConverter<TEnum, string> where TEnum : struct, Enum
{
    /// <summary>
    /// Converts an enum value to its uppercase snake_case string representation.
    /// </summary>
    /// <param name="source">The source enum value to convert.</param>
    /// <param name="destination">The destination string value (not used in conversion).</param>
    /// <param name="context">The resolution context provided by AutoMapper.</param>
    /// <returns>The uppercase snake_case string representation of the enum value.</returns>
    public string Convert(TEnum source, string destination, ResolutionContext context)
    {
        return source.WriteEnumSnakeUpper();
    }
}
