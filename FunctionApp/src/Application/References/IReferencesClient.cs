using Application.Dtos.References;


namespace Application.References;

/// <summary>
/// Defines the contract for retrieving reference data from the Genesys Cloud API.
/// </summary>
/// <remarks>
/// This interface abstracts the external API calls required to fetch reference entities such as Skills and Presence Definitions.
/// Implementations are expected to handle OAuth token management, pagination, rate limiting, and error handling transparently.
/// </remarks>
public interface IReferencesClient
{
    /// <summary>
    /// Retrieves all Skills from the Genesys Cloud API for the current Line of Business (LOB).
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <returns>A list of <see cref="SkillResponse"/> objects representing all available skills in the organization.</returns>
    Task<List<SkillResponse>> GetSkillsAsync(CancellationToken ct);

    /// <summary>
    /// Retrieves all Presence Definitions from the Genesys Cloud API for the current Line of Business (LOB).
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <returns>
    /// A list of <see cref="PresenceDefinitionResponse"/> objects representing all presence states
    /// (e.g., Available, Away, Busy) configured in the organization.
    /// </returns>
    Task<List<PresenceDefinitionResponse>> GetPresenceDefinitionsAsync(CancellationToken ct);

    /// <summary>
    /// Retrieves all Groups from the Genesys Cloud API for the current Line of Business (LOB).
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <returns>A list of <see cref="GroupResponse"/> objects representing all available groups in the organization.</returns>
    Task<List<GroupResponse>> GetGroupsAsync(CancellationToken ct);

    /// <summary>
    /// Retrieves all WrapupCodes from the Genesys Cloud API for the current Line of Business (LOB).
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <returns>A list of <see cref="WrapupCodeResponse"/> objects representing all available wrapup_codes in the organization.</returns>
    Task<List<WrapupCodeResponse>> GetWrapupCodesAsync(CancellationToken ct);
}
