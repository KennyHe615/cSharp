using Application.Abstractions.External;
using Application.Contracts.ExternalApis.Genesys.References;

using AutoMapper;

using Infrastructure.ExternalApis.Providers.Genesys.Configuration;
using Infrastructure.ExternalApis.Providers.Genesys.Modules.References.Contracts;
using Infrastructure.ExternalApis.Providers.Genesys.Transport;
using Infrastructure.ExternalApis.Shared.Http;

using Microsoft.Extensions.Options;

using System.Net;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.References;

/// <summary>
/// Genesys reference-data client for groups, skills, presence definitions, and wrap-up codes etc.
/// </summary>
public sealed class ReferencesClient : IReferenceApiClient
{
    private const int MaxPaginationIterations = 100;

    private const string SkillsEndpoint = "/api/v2/routing/skills";
    private const string PresenceDefinitionsEndpoint = "/api/v2/presence/definitions";
    private const string GroupsEndpoint = "/api/v2/groups";
    private const string WrapUpCodesEndpoint = "/api/v2/routing/wrapupcodes";

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

    /// <inheritdoc />
    public Task<IReadOnlyCollection<SkillRawContract>> GetSkillsAsync(CancellationToken ct = default)
    {
        return GetMappedReferencesAsync<SkillResponse, SkillRawContract>(SkillsEndpoint, "Skill", ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<PresenceDefinitionRawContract>> GetPresenceDefinitionsAsync(
        CancellationToken ct = default)
    {
        return
            GetMappedReferencesAsync<PresenceDefinitionResponse,
                PresenceDefinitionRawContract>(PresenceDefinitionsEndpoint, "PresenceDefinition", ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<GroupRawContract>> GetGroupsAsync(CancellationToken ct = default)
    {
        return GetMappedReferencesAsync<GroupResponse, GroupRawContract>(GroupsEndpoint, "Group", ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<WrapUpCodeRawContract>> GetWrapUpCodesAsync(CancellationToken ct = default)
    {
        return GetMappedReferencesAsync<WrapUpCodeResponse, WrapUpCodeRawContract>(WrapUpCodesEndpoint,
         "WrapUpCode",
         ct);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Fetches a paged Genesys reference endpoint and maps provider DTOs to application raw contracts.
    /// </summary>
    private async Task<IReadOnlyCollection<TContract>> GetMappedReferencesAsync<TProvider, TContract>(
        string endpointPath,
        string entityName,
        CancellationToken ct)
    {
        string initialUrl = BuildInitialUrl(endpointPath);

        IReadOnlyCollection<TProvider> provider = await GetPaginatedAsync<TProvider>(initialUrl, entityName, ct)
           .ConfigureAwait(false);

        return _mapper.Map<List<TContract>>(provider);
    }

    /// <summary>
    /// Builds the first paged request URL from endpoint path and configured page size.
    /// </summary>
    private string BuildInitialUrl(string endpointPath)
    {
        return string.IsNullOrWhiteSpace(endpointPath)
            ? throw new ArgumentException("Endpoint path must be provided.", nameof(endpointPath))
            : $"{endpointPath}?pageSize={_options.DefaultPageSize}";
    }

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
                throw new ExternalServiceHttpException(HttpStatusCode.InternalServerError,
                                                       "GET",
                                                       currentUrl,
                                                       $"Exceeded pagination limit ({MaxPaginationIterations}) for {entityName}.");
            }

            PagedReferenceResponse<T>? response =
                await _genesysApiClient.GetAsync<PagedReferenceResponse<T>>(currentUrl, ct: ct)
                                       .ConfigureAwait(false);

            if (response?.Entities is null)
            {
                throw new ExternalServiceHttpException(HttpStatusCode.OK,
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
