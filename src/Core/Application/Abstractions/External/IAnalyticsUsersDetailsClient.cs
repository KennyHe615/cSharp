using Application.Contracts.ExternalApis.Genesys.UsersDetails;


namespace Application.Abstractions.External;

public interface IAnalyticsUsersDetailsClient
{
    Task<UsersDetailsRawContract> GetUsersDetailsAsync(string intervalIso8601,
                                                       int pageNumber,
                                                       int? pageSize = null,
                                                       CancellationToken ct = default);

    Task<int> GetHitCountAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default);
}
