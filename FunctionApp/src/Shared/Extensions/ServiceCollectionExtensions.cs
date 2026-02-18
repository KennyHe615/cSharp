using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using Shared.Time;


namespace Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSharedServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Add shared JSON serializer options
        services.AddSingleton(_ =>
                              {
                                  JsonSerializerOptions options = new()
                                                                  {
                                                                      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                      PropertyNameCaseInsensitive = true
                                                                  };

                                  options.AddFlexibleSnakeUpperEnums();

                                  return options;
                              });
    }
}
