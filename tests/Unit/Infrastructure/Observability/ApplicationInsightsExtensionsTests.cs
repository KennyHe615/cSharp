using Infrastructure.Configuration;
using Infrastructure.Configuration.Options;
using Infrastructure.Observability;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Xunit;


namespace tests.Unit.Infrastructure.Observability;

public sealed class ApplicationInsightsExtensionsTests
{
    private const string AiProviderName =
        "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider";

    [Fact]
    public void AddApplicationInsightsForWorker_BindsOptions()
    {
        using ServiceProvider provider = BuildProvider(enableEfCommandLogging: false);

        ApplicationInsightsOptions options = provider.GetRequiredService<IOptions<ApplicationInsightsOptions>>()
                                                     .Value;

        Assert.Equal("InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://example",
                     options.ConnectionString);
        Assert.False(options.EnableAdaptiveSampling);
        Assert.False(options.EnableEfCommandLogging);
    }

    [Fact]
    public void AddApplicationInsightsForWorker_WhenEfCommandLoggingDisabled_ConfiguresEfAtWarning()
    {
        using ServiceProvider provider = BuildProvider(enableEfCommandLogging: false);

        LoggerFilterOptions filterOptions = provider.GetRequiredService<IOptions<LoggerFilterOptions>>()
                                                    .Value;

        LoggerFilterRule? efGlobalRule = FindRule(filterOptions, null, "Microsoft.EntityFrameworkCore");
        LoggerFilterRule? efAiRule = FindRule(filterOptions, AiProviderName, "Microsoft.EntityFrameworkCore");

        Assert.NotNull(efGlobalRule);
        Assert.NotNull(efAiRule);
        Assert.Equal(LogLevel.Warning, efGlobalRule.LogLevel);
        Assert.Equal(LogLevel.Warning, efAiRule.LogLevel);
    }

    [Fact]
    public void AddApplicationInsightsForWorker_WhenEfCommandLoggingEnabled_ConfiguresEfAtInformation()
    {
        using ServiceProvider provider = BuildProvider(enableEfCommandLogging: true);

        LoggerFilterOptions filterOptions = provider.GetRequiredService<IOptions<LoggerFilterOptions>>()
                                                    .Value;

        LoggerFilterRule? efGlobalRule = FindRule(filterOptions, null, "Microsoft.EntityFrameworkCore");
        LoggerFilterRule? efAiRule = FindRule(filterOptions, AiProviderName, "Microsoft.EntityFrameworkCore");

        Assert.NotNull(efGlobalRule);
        Assert.NotNull(efAiRule);
        Assert.Equal(LogLevel.Information, efGlobalRule.LogLevel);
        Assert.Equal(LogLevel.Information, efAiRule.LogLevel);
    }

    [Fact]
    public void AddApplicationInsightsForWorker_ConfiguresExpectedBaselineRules()
    {
        using ServiceProvider provider = BuildProvider(enableEfCommandLogging: false);

        LoggerFilterOptions filterOptions = provider.GetRequiredService<IOptions<LoggerFilterOptions>>()
                                                    .Value;

        LoggerFilterRule? microsoftRule = FindRule(filterOptions, null, "Microsoft");
        LoggerFilterRule? appRule = FindRule(filterOptions, null, "MyApp");
        LoggerFilterRule? aiBaselineRule = FindRule(filterOptions, AiProviderName, null);

        Assert.NotNull(microsoftRule);
        Assert.NotNull(appRule);
        Assert.NotNull(aiBaselineRule);

        Assert.Equal(LogLevel.Error, microsoftRule.LogLevel);
        Assert.Equal(LogLevel.Debug, appRule.LogLevel);
        Assert.Equal(LogLevel.Information, aiBaselineRule.LogLevel);
    }

    #region ========== *** Private Section *** ==========

    private static ServiceProvider BuildProvider(bool enableEfCommandLogging)
    {
        Dictionary<string, string?> settings = new Dictionary<string, string?>
                                               {
                                                   ["ApplicationInsights:ConnectionString"] =
                                                       "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://example",
                                                   ["ApplicationInsights:EnableAdaptiveSampling"] =
                                                       "false",
                                                   ["ApplicationInsights:EnableEfCommandLogging"] =
                                                       enableEfCommandLogging.ToString()
                                               };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings)
                                                                 .Build();

        ServiceCollection services = [];
        services.AddConfiguration(configuration);
        services.AddApplicationInsightsForWorker("MyApp");

        return services.BuildServiceProvider();
    }

    private static LoggerFilterRule? FindRule(LoggerFilterOptions options, string? providerName, string? categoryName)
    {
        return options.Rules.LastOrDefault(rule => rule.ProviderName    == providerName
                                                   && rule.CategoryName == categoryName);
    }

    #endregion
}
