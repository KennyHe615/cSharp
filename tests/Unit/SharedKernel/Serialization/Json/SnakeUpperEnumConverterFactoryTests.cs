using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using SharedKernel.Serialization.Json;

using Xunit;


namespace tests.Unit.SharedKernel.Serialization.Json;

public sealed class SnakeUpperEnumConverterFactoryTests
{
    private static readonly Type FactoryType =
        typeof(EnumJsonExtensions).Assembly.GetType("SharedKernel.Serialization.Json.SnakeUpperEnumConverterFactory",
                                                    true)!;

    private enum TestStatus
    {
        InQueue,
        OnBreak
    }

    #region CanConvert

    [Fact]
    public void CanConvert_ReturnsTrue_ForEnumType()
    {
        object factory = Activator.CreateInstance(FactoryType)!;

        object? raw =
            FactoryType.InvokeMember("CanConvert",
                                     BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public,
                                     null,
                                     factory,
                                     [typeof(TestStatus)]);

        bool result = Assert.IsType<bool>(raw);
        Assert.True(result);
    }

    [Fact]
    public void CanConvert_ReturnsTrue_ForNullableEnumType()
    {
        object factory = Activator.CreateInstance(FactoryType)!;

        object? raw = FactoryType.InvokeMember("CanConvert",
                                               BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public,
                                               null,
                                               factory,
                                               [typeof(TestStatus?)]);

        bool result = Assert.IsType<bool>(raw);
        Assert.True(result);
    }

    [Fact]
    public void CanConvert_ReturnsFalse_ForNonEnumType()
    {
        object factory = Activator.CreateInstance(FactoryType)!;

        object? raw = FactoryType.InvokeMember("CanConvert",
                                               BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public,
                                               null,
                                               factory,
                                               [typeof(string)]);

        bool result = Assert.IsType<bool>(raw);
        Assert.False(result);
    }

    #endregion

    #region CreateConverter

    [Fact]
    public void CreateConverter_ReturnsSnakeUpperEnumConverter_ForEnumType()
    {
        object factory = Activator.CreateInstance(FactoryType)!;
        JsonSerializerOptions options = new JsonSerializerOptions();

        object? raw = FactoryType.InvokeMember("CreateConverter",
                                               BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public,
                                               null,
                                               factory,
                                               [typeof(TestStatus), options]);

        JsonConverter converter = Assert.IsAssignableFrom<JsonConverter>(raw);
        Assert.Equal("SnakeUpperEnumConverter`1", converter.GetType().Name);
    }

    [Fact]
    public void CreateConverter_ReturnsNullableSnakeUpperEnumConverter_ForNullableEnumType()
    {
        object factory = Activator.CreateInstance(FactoryType)!;
        JsonSerializerOptions options = new JsonSerializerOptions();

        object? raw = FactoryType.InvokeMember("CreateConverter",
                                               BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public,
                                               null,
                                               factory,
                                               [typeof(TestStatus?), options]);

        JsonConverter converter = Assert.IsAssignableFrom<JsonConverter>(raw);
        Assert.Equal("NullableSnakeUpperEnumConverter`1", converter.GetType().Name);
    }

    [Fact]
    public void CreateConverter_Throws_ForNonEnumType()
    {
        object factory = Activator.CreateInstance(FactoryType)!;
        JsonSerializerOptions options = new JsonSerializerOptions();

        TargetInvocationException ex =
            Assert.Throws<TargetInvocationException>(() => FactoryType.InvokeMember("CreateConverter",
                                                      BindingFlags.InvokeMethod
                                                      | BindingFlags.Instance
                                                      | BindingFlags.Public,
                                                      null,
                                                      factory,
                                                      [typeof(string), options]));

        InvalidOperationException inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Contains("not an enum or nullable enum", inner.Message);
    }

    #endregion
}
