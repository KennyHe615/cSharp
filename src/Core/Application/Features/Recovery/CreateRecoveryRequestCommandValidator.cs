using FluentValidation;


namespace Application.Features.Recovery;

/// <summary>
/// Validates business invariants for <see cref="CreateRecoveryRequestCommand"/>.
/// </summary>
public sealed class CreateRecoveryRequestCommandValidator : AbstractValidator<CreateRecoveryRequestCommand>
{
    /// <summary>
    /// Initializes validation rules for recovery command requests.
    /// </summary>
    public CreateRecoveryRequestCommandValidator()
    {
        RuleFor(x => x)
           .Must(HaveIntervalOrGenesysJobId)
           .WithMessage("Either Interval or GenesysJobId must be provided.");

        RuleFor(x => x)
           .Must(NotHaveBothIntervalAndGenesysJobId)
           .WithMessage("Provide either Interval or GenesysJobId, not both.");

        RuleFor(x => x.GenesysJobId)
           .MaximumLength(100)
           .WithMessage("GenesysJobId cannot exceed 100 characters.")
           .When(x => !string.IsNullOrWhiteSpace(x.GenesysJobId));

        RuleFor(x => x.GenesysJobId)
           .Must(NotHaveLeadingOrTrailingSpaces)
           .WithMessage("GenesysJobId must not contain leading or trailing spaces.")
           .When(x => !string.IsNullOrWhiteSpace(x.GenesysJobId));
    }

    #region ========== *** Private Methods *** ==========

    private static bool HaveIntervalOrGenesysJobId(CreateRecoveryRequestCommand request)
    {
        bool hasInterval = request.Interval is not null;
        bool hasGenesysJobId = !string.IsNullOrWhiteSpace(request.GenesysJobId);

        return hasInterval || hasGenesysJobId;
    }

    private static bool NotHaveBothIntervalAndGenesysJobId(CreateRecoveryRequestCommand request)
    {
        bool hasInterval = request.Interval is not null;
        bool hasGenesysJobId = !string.IsNullOrWhiteSpace(request.GenesysJobId);

        return !(hasInterval && hasGenesysJobId);
    }

    private static bool NotHaveLeadingOrTrailingSpaces(string? genesysJobId)
    {
        return string.IsNullOrWhiteSpace(genesysJobId)
               || string.Equals(genesysJobId, genesysJobId.Trim(), StringComparison.Ordinal);
    }

    #endregion
}
