namespace FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

public interface IHttpClient
{
    string BaseUrl { get; }

    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint,
                                                    TRequest payload,
                                                    CancellationToken cancellationToken = default);

    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint,
                                                   TRequest payload,
                                                   CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
}
