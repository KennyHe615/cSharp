using Application.Enums;
using Application.Features.Shared;

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

        this.AddRecoverySelectorRules(x => !string.IsNullOrWhiteSpace(x.Interval),
                                      x => x.GenesysJobId,
                                      x => x.Category == SyncAnalyticsCategory.ConversationsDetails);

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

    /// <summary>
    /// Validates the textual UTC interval format.
    /// </summary>
    private static bool BeValidUtcInterval(string? interval)
    {
        return UtcInterval.TryParse(interval, out _);
    }

    #endregion
}
