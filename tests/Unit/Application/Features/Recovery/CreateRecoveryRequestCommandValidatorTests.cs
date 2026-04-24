using Application.Abstractions.Recovery;
using Application.Contracts.InternalApis.Recovery;
using Application.Features.Recovery;

using FluentValidation.TestHelper;

using Moq;

using SharedKernel.Lobs;
using SharedKernel.Time;

using tests.TestSupport.Time;

using Xunit;


namespace tests.Unit.Application.Features.Recovery;

public sealed class CreateRecoveryRequestCommandValidatorTests
{
    private readonly CreateRecoveryRequestCommandValidator _sut =
                    new CreateRecoveryRequestCommandValidator(CreatePolicy()
                                                                             .Object);

    [Fact]
    public void Validate_WithIntervalOnly_ShouldNotHaveValidationError()
    {
        CreateRecoveryRequestCommand command =
                        new CreateRecoveryRequestCommand(new LobName("CRC"),
                                                         RecoveryCategory.UsersDetails,
                                                         UtcIntervalTestFactory.Create(),
                                                         null);

        TestValidationResult<CreateRecoveryRequestCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithGenesysJobIdOnly_ShouldNotHaveValidationError()
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
    public void Validate_WithSecondPrecisionInterval_ShouldHaveValidationError()
    {
        UtcInterval interval =
                        UtcIntervalTestFactory.Create(new DateTimeOffset(2025,
                                                                         1,
                                                                         1,
                                                                         0,
                                                                         0,
                                                                         30,
                                                                         TimeSpan.Zero),
                                                      new DateTimeOffset(2025,
                                                                         1,
                                                                         1,
                                                                         1,
                                                                         0,
                                                                         0,
                                                                         TimeSpan.Zero));

        CreateRecoveryRequestCommand command =
                        new CreateRecoveryRequestCommand(new LobName("CRC"),
                                                         RecoveryCategory.UsersDetails,
                                                         interval,
                                                         null);

        TestValidationResult<CreateRecoveryRequestCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval)
              .WithErrorMessage("Interval must use minute precision: yyyy-MM-ddTHH:mmZ/yyyy-MM-ddTHH:mmZ.");
    }

    [Fact]
    public void Validate_WhenIntervalStartOutsideRetention_ShouldHaveValidationError()
    {
        Mock<IRecoveryIntervalPolicy> policy = CreatePolicy(startWithinRetention: false);
        CreateRecoveryRequestCommandValidator sut = new CreateRecoveryRequestCommandValidator(policy.Object);

        CreateRecoveryRequestCommand command =
                        new CreateRecoveryRequestCommand(new LobName("CRC"),
                                                         RecoveryCategory.UsersDetails,
                                                         UtcIntervalTestFactory.Create(),
                                                         null);

        TestValidationResult<CreateRecoveryRequestCommand> result = sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval)
              .WithErrorMessage("Interval start cannot be older than 558 days.");
    }

    [Fact]
    public void Validate_WhenIntervalEndOutsideFutureSkew_ShouldHaveValidationError()
    {
        Mock<IRecoveryIntervalPolicy> policy = CreatePolicy(endWithinFutureSkew: false);
        CreateRecoveryRequestCommandValidator sut = new CreateRecoveryRequestCommandValidator(policy.Object);

        CreateRecoveryRequestCommand command =
                        new CreateRecoveryRequestCommand(new LobName("CRC"),
                                                         RecoveryCategory.UsersDetails,
                                                         UtcIntervalTestFactory.Create(),
                                                         null);

        TestValidationResult<CreateRecoveryRequestCommand> result = sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval)
              .WithErrorMessage("Interval end cannot be more than 1 day(s) in the future.");
    }

    #region ========== *** Private Section *** ==========

    private static Mock<IRecoveryIntervalPolicy> CreatePolicy(bool startWithinRetention = true,
                                                              bool endWithinFutureSkew = true)
    {
        Mock<IRecoveryIntervalPolicy> policy = new Mock<IRecoveryIntervalPolicy>(MockBehavior.Strict);

        policy.SetupGet(x => x.HistoricalDataLimitDays)
              .Returns(558);
        policy.SetupGet(x => x.FutureSkewDays)
              .Returns(1);
        policy.Setup(x => x.IsStartWithinRetention(It.IsAny<DateTimeOffset>()))
              .Returns(startWithinRetention);
        policy.Setup(x => x.IsEndWithinFutureSkew(It.IsAny<DateTimeOffset>()))
              .Returns(endWithinFutureSkew);

        return policy;
    }

    #endregion
}
