using System.Text.Json;
using System.Text.Json.Serialization;

using SharedKernel.Serialization.Json;

using Xunit;


namespace tests.Unit.SharedKernel.Serialization.Json;

public sealed class EnumJsonExtensionsTests
{
    [Fact]
    public void AddSnakeUpperEnums_Throws_WhenOptionsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => EnumJsonExtensions.AddSnakeUpperEnums(null!));
    }

    [Fact]
    public void AddSnakeUpperEnums_ReturnsSameInstance()
    {
        JsonSerializerOptions options = new JsonSerializerOptions();

        JsonSerializerOptions returned = options.AddSnakeUpperEnums();

        Assert.Same(options, returned);
    }

    [Fact]
    public void AddSnakeUpperEnums_AddsFactory_WhenMissing()
    {
        JsonSerializerOptions options = new JsonSerializerOptions();

        options.AddSnakeUpperEnums();

        Assert.Contains(options.Converters, c => c.GetType().Name == "SnakeUpperEnumConverterFactory");
    }

    [Fact]
    public void AddSnakeUpperEnums_DoesNotDuplicateFactory_WhenCalledMultipleTimes()
    {
        JsonSerializerOptions options = new JsonSerializerOptions();

        options.AddSnakeUpperEnums();
        options.AddSnakeUpperEnums();

        int factoryCount = options.Converters.Count(c => c.GetType().Name == "SnakeUpperEnumConverterFactory");

        Assert.Equal(1, factoryCount);
    }

    [Fact]
    public void AddSnakeUpperEnums_PreservesExistingConverters()
    {
        JsonSerializerOptions options = new JsonSerializerOptions();
        JsonStringEnumConverter existing = new JsonStringEnumConverter();

        options.Converters.Add(existing);

        options.AddSnakeUpperEnums();

        Assert.Contains(existing, options.Converters);
        Assert.Contains(options.Converters, c => c.GetType().Name == "SnakeUpperEnumConverterFactory");
    }
}
