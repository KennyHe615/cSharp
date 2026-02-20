using Infrastructure.Configuration;
using Infrastructure.Configuration.Options;
using Infrastructure.Observability;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Xunit;


namespace Tests.Integration;

public sealed class ApplicationInsightsExtensionsTests
{
    [Fact]
    public void AddApplicationInsightsForWorker_BindsOptions()
    {
        Dictionary<string, string?> settings = new Dictionary<string, string?>
                                               {
                                                   ["ApplicationInsights:ConnectionString"] =
                                                       "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://example",
                                                   ["ApplicationInsights:EnableAdaptiveSampling"] =
                                                       "false"
                                               };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        ServiceCollection services = new ServiceCollection();
        services.AddConfiguration(configuration);
        services.AddApplicationInsightsForWorker("123");

        using ServiceProvider provider = services.BuildServiceProvider();

        ApplicationInsightsOptions options = provider.GetRequiredService<IOptions<ApplicationInsightsOptions>>().Value;

        Assert.Equal(settings["ApplicationInsights:ConnectionString"], options.ConnectionString);
        Assert.False(options.EnableAdaptiveSampling);
    }
}
