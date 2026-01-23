namespace FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

public interface IFlurlHttpClient
{
    string BaseUrl { get; }

    Task<T?> GetAsync<T>(string endpoint,
                         Dictionary<string, string>? headers = null,
                         CancellationToken cancellationToken = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint,
                                                    TRequest payload,
                                                    Dictionary<string, string>? headers = null,
                                                    CancellationToken cancellationToken = default);

    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint,
                                                   TRequest payload,
                                                   Dictionary<string, string>? headers = null,
                                                   CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string endpoint,
                           Dictionary<string, string>? headers = null,
                           CancellationToken cancellationToken = default);
}
