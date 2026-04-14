using Application.Contracts.ExternalApis.Genesys.References;


namespace Application.Abstractions.External;

/// <summary>
/// Defines operations for retrieving Genesys reference data for the current line of business.
/// </summary>
public interface IReferenceApiClient
{
    /// <summary>
    /// Retrieves all skills.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only collection of skill raw contracts.</returns>
    Task<IReadOnlyCollection<SkillRawContract>> GetSkillsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves all presence definitions.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only collection of presence definition raw contracts.</returns>
    Task<IReadOnlyCollection<PresenceDefinitionRawContract>>
        GetPresenceDefinitionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves all groups.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only collection of group raw contracts.</returns>
    Task<IReadOnlyCollection<GroupRawContract>> GetGroupsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves all wrap-up codes.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only collection of wrap-up code raw contracts.</returns>
    Task<IReadOnlyCollection<WrapUpCodeRawContract>> GetWrapUpCodesAsync(CancellationToken ct = default);
}
