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
        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("CRC"),
                                             RecoveryCategory.UsersDetails,
                                             BuildInterval(),
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
    public void Validate_WithMissingIntervalAndGenesysJobId_ShouldHaveValidationError()
    {
        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("NTT"),
                                             RecoveryCategory.ConversationsAggregates,
                                             null,
                                             null);

        TestValidationResult<CreateRecoveryRequestCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Either Interval or GenesysJobId must be provided.");
    }

    [Fact]
    public void Validate_WithWhitespaceGenesysJobIdAndNoInterval_ShouldHaveValidationError()
    {
        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("CRC"),
                                             RecoveryCategory.UsersDetails,
                                             null,
                                             "   ");

        TestValidationResult<CreateRecoveryRequestCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Either Interval or GenesysJobId must be provided.");
    }

    [Fact]
    public void Validate_WithBothIntervalAndGenesysJobId_ShouldHaveValidationError()
    {
        CreateRecoveryRequestCommand command = new CreateRecoveryRequestCommand(new LobName("CRC"),
                                                                                RecoveryCategory.UsersDetails,
                                                                                BuildInterval(),
                                                                                "JOB-123");

        TestValidationResult<CreateRecoveryRequestCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Provide either Interval or GenesysJobId, not both.");
    }

    [Fact]
    public void Validate_WithGenesysJobIdLongerThan100_ShouldHaveValidationError()
    {
        string longJobId = new string('A', 101);

        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("CRC"),
                                             RecoveryCategory.UsersDetails,
                                             null,
                                             longJobId);

        TestValidationResult<CreateRecoveryRequestCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GenesysJobId)
              .WithErrorMessage("GenesysJobId cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithGenesysJobIdAt100Chars_ShouldNotHaveValidationError()
    {
        string boundaryJobId = new string('A', 100);

        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("CRC"),
                                             RecoveryCategory.UsersDetails,
                                             null,
                                             boundaryJobId);

        TestValidationResult<CreateRecoveryRequestCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.GenesysJobId);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(" JOB-123")]
    [InlineData("JOB-123 ")]
    public void Validate_WithLeadingOrTrailingSpacesInGenesysJobId_ShouldHaveValidationError(string genesysJobId)
    {
        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("CRC"),
                                             RecoveryCategory.UsersDetails,
                                             null,
                                             genesysJobId);

        TestValidationResult<CreateRecoveryRequestCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GenesysJobId)
              .WithErrorMessage("GenesysJobId must not contain leading or trailing spaces.");
    }

    [Fact]
    public void Validate_WithTrimmedGenesysJobId_ShouldNotHaveValidationError()
    {
        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("CRC"),
                                             RecoveryCategory.UsersDetails,
                                             null,
                                             "JOB-123");

        TestValidationResult<CreateRecoveryRequestCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.GenesysJobId);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #region ========== *** Private Section *** ==========

    private static UtcInterval BuildInterval()
    {
        return new UtcInterval(new DateTimeOffset(2025,
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
                                                  TimeSpan.Zero));
    }

    #endregion
}
