using FluentValidation;

using SharedKernel.Time;


namespace Application.Features.SyncTracking.Analytics;

/// <summary>
/// Validates <see cref="RunAnalyticsRecoverySyncCommand"/> input rules.
/// </summary>
public sealed class RunAnalyticsRecoverySyncCommandValidator : AbstractValidator<RunAnalyticsRecoverySyncCommand>
{
    /// <summary>
    /// Configures validation rules for recovery sync requests.
    /// </summary>
    public RunAnalyticsRecoverySyncCommandValidator()
    {
        RuleFor(x => x.Category)
                       .IsInEnum()
                       .WithMessage("Category is invalid.");

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

        RuleFor(x => x.Interval)
                       .Must(BeValidUtcInterval)
                       .When(x => !string.IsNullOrWhiteSpace(x.Interval))
                       .WithMessage("Interval format is invalid. Expected UTC interval: yyyy-MM-ddTHH:mmZ/yyyy-MM-ddTHH:mmZ.");

        RuleFor(x => x.Interval)
                       .MaximumLength(50)
                       .WithMessage("Interval cannot exceed 50 characters.")
                       .When(x => !string.IsNullOrWhiteSpace(x.Interval));

        RuleFor(x => x.PageNumber)
                       .GreaterThanOrEqualTo(1)
                       .When(x => x.PageNumber.HasValue)
                       .WithMessage("PageNumber must be greater than or equal to 1 when provided.");

        RuleFor(x => x.Category)
                       .Must(AnalyticsCategoryGuards.IsAnalyticsCategory)
                       .WithMessage("Category is not supported for recovery sync.");
    }

    #region ========== *** Private Section *** ==========

    private static bool HaveIntervalOrGenesysJobId(RunAnalyticsRecoverySyncCommand request)
    {
        bool hasInterval = !string.IsNullOrWhiteSpace(request.Interval);
        bool hasGenesysJobId = !string.IsNullOrWhiteSpace(request.GenesysJobId);

        return hasInterval || hasGenesysJobId;
    }

    private static bool NotHaveBothIntervalAndGenesysJobId(RunAnalyticsRecoverySyncCommand request)
    {
        bool hasInterval = !string.IsNullOrWhiteSpace(request.Interval);
        bool hasGenesysJobId = !string.IsNullOrWhiteSpace(request.GenesysJobId);

        return !(hasInterval && hasGenesysJobId);
    }

    private static bool NotHaveLeadingOrTrailingSpaces(string? genesysJobId)
    {
        return string.IsNullOrWhiteSpace(genesysJobId)
               || string.Equals(genesysJobId, genesysJobId.Trim(), StringComparison.Ordinal);
    }

    private static bool BeValidUtcInterval(string? interval)
    {
        return UtcInterval.TryParse(interval, out _);
    }

    #endregion
}
