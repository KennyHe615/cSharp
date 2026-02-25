using SharedKernel.Environment;

using Xunit;


namespace tests.Unit.SharedKernel.Environment;

public sealed class AppEnvironmentTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromHostEnvironment_ReturnsDevelopment_WhenInputIsNullOrWhitespace(string? input)
    {
        AppEnvironment actual = AppEnvironment.FromHostEnvironment(input);

        Assert.Equal(AppEnvironmentKind.Development, actual.Kind);
        Assert.Equal("dev", actual.Alias);
    }

    [Theory]
    [InlineData("development")]
    [InlineData("dev")]
    [InlineData("local")]
    [InlineData(" Development ")]
    public void FromHostEnvironment_ReturnsDevelopment_ForDevelopmentAliases(string input)
    {
        AppEnvironment actual = AppEnvironment.FromHostEnvironment(input);

        Assert.Equal(AppEnvironmentKind.Development, actual.Kind);
        Assert.Equal("dev", actual.Alias);
    }

    [Theory]
    [InlineData("uat")]
    [InlineData("stage")]
    [InlineData("stg")]
    [InlineData(" STAGE ")]
    public void FromHostEnvironment_ReturnsUat_ForUatAliases(string input)
    {
        AppEnvironment actual = AppEnvironment.FromHostEnvironment(input);

        Assert.Equal(AppEnvironmentKind.Uat, actual.Kind);
        Assert.Equal("uat", actual.Alias);
    }

    [Theory]
    [InlineData("production")]
    [InlineData("prod")]
    [InlineData(" PROD ")]
    public void FromHostEnvironment_ReturnsProduction_ForProductionAliases(string input)
    {
        AppEnvironment actual = AppEnvironment.FromHostEnvironment(input);

        Assert.Equal(AppEnvironmentKind.Production, actual.Kind);
        Assert.Equal("prod", actual.Alias);
    }

    [Fact]
    public void FromHostEnvironment_Throws_WhenUnsupported()
    {
        const string input = "qa";

        InvalidOperationException ex =
            Assert.Throws<InvalidOperationException>(() => AppEnvironment.FromHostEnvironment(input));

        Assert.Contains("Unsupported environment", ex.Message);
        Assert.Contains("Allowed", ex.Message);
    }
}
