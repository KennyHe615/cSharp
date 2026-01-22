namespace FunctionApp.Infrastructure.Security;

public interface ITokenProvider
{
    Task<string> GetValidTokenAsync(CancellationToken cancellationToken = default);

    Task RefreshTokenAsync(CancellationToken cancellationToken = default);
}
