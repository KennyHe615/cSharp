using Application.Abstractions.External;
using Application.Contracts.ExternalApis.Genesys.UsersDetails;

using AutoMapper;

using Infrastructure.ExternalApis.Providers.Genesys.Configuration;
using Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.UsersDetails.Contracts;
using Infrastructure.ExternalApis.Providers.Genesys.Transport;
using Infrastructure.ExternalApis.Shared.Http;

using Microsoft.Extensions.Options;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.UsersDetails;

/// <summary>
/// Genesys Analytics client for Users Details endpoint.
/// Maps provider contracts into application raw contracts.
/// </summary>
public sealed class UsersDetailsClient : IAnalyticsUsersDetailsClient
{
    private const string UsersDetailsQueryEndpoint = "/api/v2/analytics/users/details/query";

    private readonly IGenesysApiClient _genesysApiClient;
    private readonly GenesysOptions _options;
    private readonly IMapper _mapper;

    public UsersDetailsClient(IGenesysApiClient genesysApiClient, IOptions<GenesysOptions> options, IMapper mapper)
    {
        _genesysApiClient = genesysApiClient ?? throw new ArgumentNullException(nameof(genesysApiClient));
        ArgumentNullException.ThrowIfNull(options);
        _mapper = mapper         ?? throw new ArgumentNullException(nameof(mapper));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<UsersDetailsRawContract> GetUsersDetailsAsync(string intervalIso8601,
                                                                    int pageNumber,
                                                                    int? pageSize = null,
                                                                    CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(intervalIso8601))
        {
            throw new ArgumentException("Interval must be provided.", nameof(intervalIso8601));
        }

        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), pageNumber, "Page number must be >= 1.");
        }

        int resolvedPageSize = pageSize ?? _options.DefaultPageSize;
        if (resolvedPageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), resolvedPageSize, "Page size must be >= 1.");
        }

        UsersDetailsRequest payload = new UsersDetailsRequest
                                      {
                                          Order = _options.DefaultQueryOrder,
                                          Interval = intervalIso8601,
                                          Paging = new Paging
                                                   {
                                                       PageNumber = pageNumber,
                                                       PageSize = resolvedPageSize
                                                   }
                                      };

        UsersDetailsResponse? response =
            await _genesysApiClient
                 .PostAsync<UsersDetailsRequest, UsersDetailsResponse>(UsersDetailsQueryEndpoint, payload, ct: ct)
                 .ConfigureAwait(false);

        if (response == null)
        {
            throw new ExternalServiceHttpException(System.Net.HttpStatusCode.OK,
                                                   "POST",
                                                   UsersDetailsQueryEndpoint,
                                                   "Genesys Users Details response body was null.");
        }

        return _mapper.Map<UsersDetailsRawContract>(response);
    }

    public async Task<int> GetHitCountAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default)
    {
        if (end <= start)
        {
            throw new ArgumentException("End must be greater than start.", nameof(end));
        }

        string interval = $"{start:O}/{end:O}";
        UsersDetailsRawContract response = await GetUsersDetailsAsync(interval,
                                                                      1,
                                                                      1,
                                                                      ct)
           .ConfigureAwait(false);

        return response.TotalHits;
    }
}
