using Application.Enums;

using FluentValidation;

using SharedKernel.Time;


namespace Application.Features.SyncTracking;

/// <summary>
/// Validates <see cref="RunIncrementalSyncCommand"/> input rules.
/// </summary>
public sealed class RunIncrementalSyncCommandValidator : AbstractValidator<RunIncrementalSyncCommand>
{
    /// <summary>
    /// Configures validation rules for incremental sync requests.
    /// </summary>
    public RunIncrementalSyncCommandValidator()
    {
        RuleFor(x => x.Category)
           .IsInEnum()
           .WithMessage("Category is invalid.");

        RuleFor(x => x.Interval)
           .NotEmpty()
           .When(x => IsAnalyticsCategory(x.Category))
           .WithMessage("Interval is required for analytics incremental sync.");

        RuleFor(x => x.Interval)
           .Must(BeValidUtcInterval)
           .When(x => IsAnalyticsCategory(x.Category) && !string.IsNullOrWhiteSpace(x.Interval))
           .WithMessage("Interval format is invalid. Expected UTC interval: yyyy-MM-ddTHH:mmZ/yyyy-MM-ddTHH:mmZ.");

        RuleFor(x => x.Interval)
           .MaximumLength(50)
           .When(x => !string.IsNullOrWhiteSpace(x.Interval))
           .WithMessage("Interval cannot exceed 50 characters.");

        RuleFor(x => x.PageNumber)
           .GreaterThanOrEqualTo(1)
           .When(x => x.PageNumber.HasValue)
           .WithMessage("PageNumber must be greater than or equal to 1 when provided.");
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Determines whether a category belongs to analytics and therefore requires an interval.
    /// </summary>
    private static bool IsAnalyticsCategory(SyncCategory category)
    {
        return category is SyncCategory.UsersDetails or SyncCategory.ConversationsDetails
                                                     or SyncCategory.ConversationsAggregates;
    }

    private static bool BeValidUtcInterval(string? interval)
    {
        return UtcInterval.TryParse(interval, out _);
    }

    #endregion
}
