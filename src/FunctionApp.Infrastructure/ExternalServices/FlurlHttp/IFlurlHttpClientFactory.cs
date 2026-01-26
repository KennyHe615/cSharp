using Flurl.Http;

using Polly.Wrap;


namespace FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

public interface IFlurlHttpClientFactory
{
    FlurlClient GetOrAddClient(string baseUrl);

    // Shared global policies
    AsyncPolicyWrap GetSafePolicy();

    AsyncPolicyWrap GetUnsafePolicy();
}
