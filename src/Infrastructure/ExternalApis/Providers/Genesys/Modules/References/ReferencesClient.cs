using Application.Abstractions.External;
using Application.Contracts.ExternalApis.Genesys.References;

using AutoMapper;

using Infrastructure.ExternalApis.Providers.Genesys.Configuration;
using Infrastructure.ExternalApis.Providers.Genesys.Modules.References.Contracts;
using Infrastructure.ExternalApis.Providers.Genesys.Transport;
using Infrastructure.ExternalApis.Shared.Http;

using Microsoft.Extensions.Options;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.References;

/// <summary>
/// Genesys reference-data client for groups, skills, presence definitions, and wrap-up codes etc.
/// </summary>
public sealed class ReferencesClient : IReferenceApiClient
{
    private const int MaxPaginationIterations = 100;

    private readonly IGenesysApiClient _genesysApiClient;
    private readonly GenesysOptions _options;
    private readonly IMapper _mapper;

    public ReferencesClient(IGenesysApiClient genesysApiClient, IOptions<GenesysOptions> options, IMapper mapper)
    {
        _genesysApiClient = genesysApiClient ?? throw new ArgumentNullException(nameof(genesysApiClient));
        ArgumentNullException.ThrowIfNull(options);
        _mapper = mapper         ?? throw new ArgumentNullException(nameof(mapper));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<IReadOnlyCollection<SkillRawContract>> GetSkillsAsync(CancellationToken ct = default)
    {
        string initialUrl = $"/api/v2/routing/skills?pageSize={_options.DefaultPageSize}";

        IReadOnlyCollection<SkillResponse> provider = await GetPaginatedAsync<SkillResponse>(initialUrl, "Skill", ct);

        return _mapper.Map<List<SkillRawContract>>(provider);
    }

    public async Task<IReadOnlyCollection<PresenceDefinitionRawContract>> GetPresenceDefinitionsAsync(
        CancellationToken ct = default)
    {
        string initialUrl = $"/api/v2/presence/definitions?pageSize={_options.DefaultPageSize}";

        IReadOnlyCollection<PresenceDefinitionResponse> provider =
            await GetPaginatedAsync<PresenceDefinitionResponse>(initialUrl, "PresenceDefinition", ct);

        return _mapper.Map<List<PresenceDefinitionRawContract>>(provider);
    }

    public async Task<IReadOnlyCollection<GroupRawContract>> GetGroupsAsync(CancellationToken ct = default)
    {
        string initialUrl = $"/api/v2/groups?pageSize={_options.DefaultPageSize}";

        IReadOnlyCollection<GroupResponse> provider = await GetPaginatedAsync<GroupResponse>(initialUrl, "Group", ct);

        return _mapper.Map<List<GroupRawContract>>(provider);
    }

    public async Task<IReadOnlyCollection<WrapUpCodeRawContract>> GetWrapUpCodesAsync(CancellationToken ct = default)
    {
        string initialUrl = $"/api/v2/routing/wrapupcodes?pageSize={_options.DefaultPageSize}";

        IReadOnlyCollection<WrapUpCodeResponse> provider =
            await GetPaginatedAsync<WrapUpCodeResponse>(initialUrl, "WrapUpCode", ct);

        return _mapper.Map<List<WrapUpCodeRawContract>>(provider);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Iterates a Genesys paged endpoint by following <c>nextUri</c> until exhausted.
    /// </summary>
    private async Task<IReadOnlyCollection<T>> GetPaginatedAsync<T>(string initialUrl,
                                                                    string entityName,
                                                                    CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(initialUrl))
        {
            throw new ArgumentException("Initial URL must be provided.", nameof(initialUrl));
        }

        List<T> results = [];
        string? currentUrl = initialUrl;
        int iterations = 0;

        while (!string.IsNullOrWhiteSpace(currentUrl))
        {
            ct.ThrowIfCancellationRequested();

            if (iterations >= MaxPaginationIterations)
            {
                throw new ExternalServiceHttpException(System.Net.HttpStatusCode.InternalServerError,
                                                       "GET",
                                                       currentUrl,
                                                       $"Exceeded pagination limit ({MaxPaginationIterations}) for {entityName}.");
            }

            PagedReferenceResponse<T>? response =
                await _genesysApiClient.GetAsync<PagedReferenceResponse<T>>(currentUrl, ct: ct)
                                       .ConfigureAwait(false);

            if (response?.Entities is null)
            {
                throw new ExternalServiceHttpException(System.Net.HttpStatusCode.OK,
                                                       "GET",
                                                       currentUrl,
                                                       $"Genesys {entityName} response payload is missing entities.");
            }

            results.AddRange(response.Entities);
            currentUrl = response.NextUri;
            iterations++;
        }

        return results;
    }

    #endregion
}
