using Application.Abstractions.Recovery;
using Application.Contracts.InternalApis.Recovery;
using Application.Features.Shared;

using FluentValidation;

using SharedKernel.Time;


namespace Application.Features.Recovery;

/// <summary>
/// Validates business invariants for <see cref="CreateRecoveryRequestCommand"/>.
/// </summary>
public sealed class CreateRecoveryRequestCommandValidator : AbstractValidator<CreateRecoveryRequestCommand>
{
    private readonly IRecoveryIntervalPolicy _recoveryIntervalPolicy;

    /// <summary>
    /// Initializes validation rules for recovery command requests.
    /// </summary>
    /// <param name="recoveryIntervalPolicy">Policy used to validate provider-backed recovery interval bounds.</param>
    public CreateRecoveryRequestCommandValidator(IRecoveryIntervalPolicy recoveryIntervalPolicy)
    {
        _recoveryIntervalPolicy =
                        recoveryIntervalPolicy ?? throw new ArgumentNullException(nameof(recoveryIntervalPolicy));

        this.AddRecoverySelectorRules(x => x.Interval is not null,
                                      x => x.GenesysJobId,
                                      x => x.Category == RecoveryCategory.ConversationsDetails);

        RuleFor(x => x.Interval)
                       .Must(BeMinutePrecision)
                       .WithMessage("Interval must use minute precision: yyyy-MM-ddTHH:mmZ/yyyy-MM-ddTHH:mmZ.")
                       .When(x => x.Interval.HasValue);

        RuleFor(x => x.Interval)
                       .Must(BeWithinHistoricalRetention)
                       .WithMessage(_ =>
                                                    $"Interval start cannot be older than {_recoveryIntervalPolicy.HistoricalDataLimitDays} days.")
                       .When(x => x.Interval.HasValue);

        RuleFor(x => x.Interval)
                       .Must(BeWithinFutureSkew)
                       .WithMessage(_ =>
                                                    $"Interval end cannot be more than {_recoveryIntervalPolicy.FutureSkewDays} day(s) in the future.")
                       .When(x => x.Interval.HasValue);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Ensures the supplied interval uses minute precision only.
    /// </summary>
    private static bool BeMinutePrecision(UtcInterval? interval)
    {
        return interval is null
               || interval.Value.Start is { Second: 0, Millisecond: 0 }
               && interval.Value.End is { Second: 0, Millisecond: 0 };
    }

    /// <summary>
    /// Ensures the interval start stays within the allowed historical retention window.
    /// </summary>
    private bool BeWithinHistoricalRetention(UtcInterval? interval)
    {
        return interval is null || _recoveryIntervalPolicy.IsStartWithinRetention(interval.Value.Start);
    }

    /// <summary>
    /// Ensures the interval end stays within the allowed future-skew window.
    /// </summary>
    private bool BeWithinFutureSkew(UtcInterval? interval)
    {
        return interval is null || _recoveryIntervalPolicy.IsEndWithinFutureSkew(interval.Value.End);
    }

    #endregion
}
