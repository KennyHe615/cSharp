using Application.Common.Enums;

using FluentValidation;

using Application.Common.Factories;
using Application.Contracts.Recovery;


namespace Application.Features.Recovery;

public class CreateRecoveryRequestCommandValidator : AbstractValidator<CreateRecoveryRequestCommand>
{
    public CreateRecoveryRequestCommandValidator()
    {
        RuleFor(x => x.Lob)
            .NotEmpty()
            .WithMessage("Lob is required")
            .Must(lob => Enum.IsDefined(typeof(RecoveryLob), lob));

        RuleFor(x => x.Category)
            .Must(category => !category.HasValue || Enum.IsDefined(typeof(SyncCategory), category.Value));

        RuleFor(x => x).Must(HaveValidIntervalOrJobId).WithMessage("Either Interval or JobId must be provided");

        RuleFor(x => x.Interval)
            .Custom((value, context) =>
                    {
                        if (string.IsNullOrWhiteSpace(value)) return;

                        try
                        {
                            IntervalFactory.FromString(value);
                        }
                        catch (ArgumentException)
                        {
                            context.AddFailure(
                                "Invalid interval format. Expected interval in UTC: 'yyyy-MM-ddTHH:mmZ/yyyy-MM-ddTHH:mm'");
                        }
                    });
    }

    private static bool HaveValidIntervalOrJobId(CreateRecoveryRequestCommand command)
    {
        return !string.IsNullOrWhiteSpace(command.Interval) || !string.IsNullOrWhiteSpace(command.JobId);
    }
}
