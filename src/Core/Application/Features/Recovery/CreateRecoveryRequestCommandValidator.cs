using Application.Abstractions.Recovery;

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

    private static bool BeMinutePrecision(UtcInterval? interval)
    {
        return interval is null
               || interval.Value.Start is { Second: 0, Millisecond: 0 }
               && interval.Value.End is { Second: 0, Millisecond: 0 };
    }

    private bool BeWithinHistoricalRetention(UtcInterval? interval)
    {
        return interval is null || _recoveryIntervalPolicy.IsStartWithinRetention(interval.Value.Start);
    }

    private bool BeWithinFutureSkew(UtcInterval? interval)
    {
        return interval is null || _recoveryIntervalPolicy.IsEndWithinFutureSkew(interval.Value.End);
    }

    #endregion
}
