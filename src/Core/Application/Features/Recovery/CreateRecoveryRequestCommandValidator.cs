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
           .Must(HaveIntervalOrJobId)
           .WithMessage("Either Interval or JobId must be provided.");
    }

    #region ========== *** Private Methods *** ==========

    private static bool HaveIntervalOrJobId(CreateRecoveryRequestCommand request)
    {
        bool hasInterval = request.Interval is not null;
        bool hasJobId = !string.IsNullOrWhiteSpace(request.JobId);

        return hasInterval || hasJobId;
    }

    #endregion
}
