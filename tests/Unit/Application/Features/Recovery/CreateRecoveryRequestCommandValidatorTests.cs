using Application.Contracts.InternalApis.Recovery;
using Application.Features.Recovery;

using FluentValidation.TestHelper;

using SharedKernel.Lobs;
using SharedKernel.Time;

using Xunit;


namespace tests.Unit.Application.Features.Recovery;

public sealed class CreateRecoveryRequestCommandValidatorTests
{
    private readonly CreateRecoveryRequestCommandValidator _sut = new CreateRecoveryRequestCommandValidator();

    [Fact]
    public void Validate_WithIntervalOnly_ShouldNotHaveValidationError()
    {
        CreateRecoveryRequestCommand command = new CreateRecoveryRequestCommand(new LobName("CRC"),
                                                                                RecoveryCategory.UsersDetails,
                                                                                new UtcInterval(new DateTimeOffset(2025,
                                                                                  1,
                                                                                  1,
                                                                                  0,
                                                                                  0,
                                                                                  0,
                                                                                  TimeSpan.Zero),
                                                                                 new DateTimeOffset(2025,
                                                                                  1,
                                                                                  1,
                                                                                  1,
                                                                                  0,
                                                                                  0,
                                                                                  TimeSpan.Zero)),
                                                                                null);

        TestValidationResult<CreateRecoveryRequestCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithJobIdOnly_ShouldNotHaveValidationError()
    {
        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("LCL"),
                                             RecoveryCategory.ConversationsDetails,
                                             null,
                                             "JOB-123");

        TestValidationResult<CreateRecoveryRequestCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMissingIntervalAndJobId_ShouldHaveValidationError()
    {
        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("NTT"),
                                             RecoveryCategory.ConversationsAggregates,
                                             null,
                                             null);

        TestValidationResult<CreateRecoveryRequestCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Either Interval or JobId must be provided.");
    }

    [Fact]
    public void Validate_WithWhitespaceJobIdAndNoInterval_ShouldHaveValidationError()
    {
        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("CRC"),
                                             RecoveryCategory.UsersDetails,
                                             null,
                                             "   ");

        TestValidationResult<CreateRecoveryRequestCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Either Interval or JobId must be provided.");
    }
}
