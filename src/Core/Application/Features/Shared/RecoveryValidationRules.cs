namespace Application.Features.Shared;

/// <summary>
/// Shared non-FluentValidation helper rules for recovery-related command handlers.
/// </summary>
public static class RecoveryValidationRules
{
    /// <summary>
    /// Returns <c>true</c> when a Genesys job identifier is either omitted or used for ConversationsDetails recovery.
    /// </summary>
    /// <param name="genesysJobId">Optional Genesys job identifier.</param>
    /// <param name="isConversationsDetailsCategory">
    /// <c>true</c> when the current request category is ConversationsDetails; otherwise <c>false</c>.
    /// </param>
    public static bool OnlyUseGenesysJobIdForConversationsDetails(string? genesysJobId,
                                                                  bool isConversationsDetailsCategory)
    {
        return string.IsNullOrWhiteSpace(genesysJobId) || isConversationsDetailsCategory;
    }

    /// <summary>
    /// Returns <c>true</c> when the supplied Genesys job identifier has no leading or trailing whitespace.
    /// </summary>
    /// <param name="genesysJobId">Optional Genesys job identifier.</param>
    public static bool NotHaveLeadingOrTrailingSpaces(string? genesysJobId)
    {
        return string.IsNullOrWhiteSpace(genesysJobId)
               || string.Equals(genesysJobId, genesysJobId.Trim(), StringComparison.Ordinal);
    }
}
