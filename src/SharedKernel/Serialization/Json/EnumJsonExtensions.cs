using System.Text.Json;


namespace SharedKernel.Serialization.Json;

/// <summary>
/// Provides JSON serializer extension methods for registering enum converters
/// that use normalized parsing and canonical <c>SNAKE_UPPER</c> output.
/// </summary>
public static class EnumJsonExtensions
{
    /// <summary>
    /// Registers enum converters that:
    /// <list type="bullet">
    /// <item><description>parse enum strings using normalized token matching,</description></item>
    /// <item><description>serialize enum values as <c>SNAKE_UPPER</c>,</description></item>
    /// <item><description>support both enum and nullable-enum types,</description></item>
    /// <item><description>avoid duplicate factory registration.</description></item>
    /// </list>
    /// </summary>
    /// <param name="options">The serializer options to configure.</param>
    /// <returns>The same <see cref="JsonSerializerOptions"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <c>null</c>.
    /// </exception>
    public static JsonSerializerOptions AddSnakeUpperEnums(this JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Converters.Any(c => c is SnakeUpperEnumConverterFactory))
        {
            options.Converters.Add(new SnakeUpperEnumConverterFactory());
        }

        return options;
    }
}
