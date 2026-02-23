using Application.Abstractions.External;
using Application.Contracts.ExternalApis.Genesys.References;

using Infrastructure.ExternalApis.Genesys.Abstractions;
using Infrastructure.ExternalApis.Http;

using Microsoft.Extensions.Options;


namespace Infrastructure.ExternalApis.Genesys.References;

/// <summary>
/// Genesys reference-data client for groups, skills, presence definitions, and wrap-up codes etc.
/// </summary>
public sealed class ReferencesClient : IReferenceApiClient
{
    private const int MaxPaginationIterations = 100;

    private readonly IGenesysApiClient _genesysApiClient;
    private readonly GenesysOptions _options;

    public ReferencesClient(IGenesysApiClient genesysApiClient, IOptions<GenesysOptions> options)
    {
        _genesysApiClient = genesysApiClient ?? throw new ArgumentNullException(nameof(genesysApiClient));
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<SkillResponse>> GetSkillsAsync(CancellationToken ct = default)
    {
        string initialUrl = $"/api/v2/routing/skills?pageSize={_options.DefaultPageSize}";

        return GetPaginatedAsync<SkillResponse>(initialUrl, "Skill", ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<PresenceDefinitionResponse>> GetPresenceDefinitionsAsync(CancellationToken ct =
        default)
    {
        string initialUrl = $"/api/v2/presence/definitions?pageSize={_options.DefaultPageSize}";

        return GetPaginatedAsync<PresenceDefinitionResponse>(initialUrl, "PresenceDefinition", ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<GroupResponse>> GetGroupsAsync(CancellationToken ct = default)
    {
        string initialUrl = $"/api/v2/groups?pageSize={_options.DefaultPageSize}";

        return GetPaginatedAsync<GroupResponse>(initialUrl, "Group", ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<WrapUpCodeResponse>> GetWrapUpCodesAsync(CancellationToken ct = default)
    {
        string initialUrl = $"/api/v2/routing/wrapupcodes?pageSize={_options.DefaultPageSize}";

        return GetPaginatedAsync<WrapUpCodeResponse>(initialUrl, "WrapUpCode", ct);
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
                await _genesysApiClient.GetAsync<PagedReferenceResponse<T>>(currentUrl, ct: ct).ConfigureAwait(false);

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
