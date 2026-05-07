using System.Linq.Expressions;

using FluentValidation;


namespace Application.Features.Shared;

/// <summary>
/// Shared validator extensions for recovery-oriented commands
/// that use interval and Genesys job-id selectors.
/// </summary>
public static class RecoveryValidatorExtensions
{
    /// <summary>
    /// Applies the shared recovery selector rules:
    /// interval-or-job-id required, mutual exclusivity, ConversationsDetails-only job-id usage,
    /// Genesys job-id max length, and Genesys job-id whitespace validation.
    /// </summary>
    /// <typeparam name="T">Validator request type.</typeparam>
    /// <param name="validator">Target validator.</param>
    /// <param name="hasInterval">Function that determines whether the request has an interval value.</param>
    /// <param name="genesysJobIdSelector">Expression selecting the Genesys job-id property.</param>
    /// <param name="isConversationsDetailsCategory">
    /// Function that determines whether the request category is ConversationsDetails.
    /// </param>
    public static void AddRecoverySelectorRules<T>(this AbstractValidator<T> validator,
                                                   Func<T, bool> hasInterval,
                                                   Expression<Func<T, string?>> genesysJobIdSelector,
                                                   Func<T, bool> isConversationsDetailsCategory)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(hasInterval);
        ArgumentNullException.ThrowIfNull(genesysJobIdSelector);
        ArgumentNullException.ThrowIfNull(isConversationsDetailsCategory);

        Func<T, string?> getGenesysJobId = genesysJobIdSelector.Compile();

        validator.RuleFor(x => x)
                 .Must(request => hasInterval(request) || !string.IsNullOrWhiteSpace(getGenesysJobId(request)))
                 .WithMessage("Either Interval or GenesysJobId must be provided.");

        validator.RuleFor(x => x)
                 .Must(request => !(hasInterval(request) && !string.IsNullOrWhiteSpace(getGenesysJobId(request))))
                 .WithMessage("Provide either Interval or GenesysJobId, not both.");

        validator.RuleFor(x => x)
                 .Must(request =>
                                       RecoveryValidationRules
                                                      .OnlyUseGenesysJobIdForConversationsDetails(getGenesysJobId(request),
                                                           isConversationsDetailsCategory(request)))
                 .WithMessage("GenesysJobId is only supported for ConversationsDetails recovery.");

        validator.RuleFor(genesysJobIdSelector)
                 .MaximumLength(100)
                 .WithMessage("GenesysJobId cannot exceed 100 characters.")
                 .When(request => !string.IsNullOrWhiteSpace(getGenesysJobId(request)));

        validator.RuleFor(genesysJobIdSelector)
                 .Must(RecoveryValidationRules.NotHaveLeadingOrTrailingSpaces)
                 .WithMessage("GenesysJobId must not contain leading or trailing spaces.")
                 .When(request => !string.IsNullOrWhiteSpace(getGenesysJobId(request)));
    }
}
