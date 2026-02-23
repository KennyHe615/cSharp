using Application.Contracts.ExternalApis.Genesys.References;


namespace Application.Abstractions.External;

/// <summary>
/// Defines the contract for retrieving reference data from the Genesys Cloud API.
/// </summary>
/// <remarks>
/// This interface abstracts the external API calls required to fetch reference entities such as Skills and Presence Definitions etc.
/// Implementations are expected to handle OAuth token management, pagination, rate limiting, and error handling transparently.
/// </remarks>
public interface IReferenceApiClient
{
    /// <summary>
    /// Retrieves all Skills from the Genesys Cloud API for the current Line of Business (LOB).
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <returns>A list of <see cref="SkillResponse"/> objects representing all available skills in the organization.</returns>
    Task<IReadOnlyCollection<SkillResponse>> GetSkillsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves all Presence Definitions from the Genesys Cloud API for the current Line of Business (LOB).
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <returns>
    /// A list of <see cref="PresenceDefinitionResponse"/> objects representing all presence states in the organization.
    /// </returns>
    Task<IReadOnlyCollection<PresenceDefinitionResponse>> GetPresenceDefinitionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves all Groups from the Genesys Cloud API for the current Line of Business (LOB).
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <returns>A list of <see cref="GroupResponse"/> objects representing all groups in the organization.</returns>
    Task<IReadOnlyCollection<GroupResponse>> GetGroupsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves all WrapUpCodes from the Genesys Cloud API for the current Line of Business (LOB).
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <returns>A list of <see cref="WrapUpCodeResponse"/> objects representing all wrap_up_codes in the organization.</returns>
    Task<IReadOnlyCollection<WrapUpCodeResponse>> GetWrapUpCodesAsync(CancellationToken ct = default);
}
