namespace SharedKernel.Environment;

/// <summary>
/// Normalized environment value object with a stable alias used in naming conventions.
/// </summary>
public sealed record AppEnvironment(AppEnvironmentKind Kind,
                                    string Alias)
{
    public static AppEnvironment FromHostEnvironment(string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return new AppEnvironment(AppEnvironmentKind.Development, "dev");
        }

        string normalized = environmentName.Trim().ToLowerInvariant();

        return normalized switch
               {
                   "development" or "dev" or "local" => new AppEnvironment(AppEnvironmentKind.Development, "dev"),
                   "uat" or "stage" or "stg" => new AppEnvironment(AppEnvironmentKind.Uat, "uat"),
                   "production" or "prod" => new AppEnvironment(AppEnvironmentKind.Production, "prod"),
                   _ => throw new
                       InvalidOperationException($"Unsupported environment '{environmentName}'. Allowed: Development/Dev, Uat, Production/Prod.")
               };
    }
}
