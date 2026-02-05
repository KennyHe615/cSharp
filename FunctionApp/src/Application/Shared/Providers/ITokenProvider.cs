namespace Application.Shared.Providers;

public interface ITokenProvider
{
    Task<string> GetValidTokenAsync(CancellationToken ct = default);

    Task RefreshTokenAsync(CancellationToken ct = default);
}
