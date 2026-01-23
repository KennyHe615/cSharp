namespace FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

public interface IFlurlHttpClientFactory
{
    IFlurlHttpClient CreateClient(string baseUrl,
                                  Func<CancellationToken, Task<string?>>? tokenProvider = null,
                                  Func<CancellationToken, Task>? refreshToken = null);
}
