using Application.Enums;
using Application.Features.Analytics.Recovery;

using FluentValidation.TestHelper;

using Xunit;


namespace tests.Unit.Application.Features.Analytics.Recovery;

public sealed class RunAnalyticsRecoverySyncCommandValidatorTests
{
    private readonly RunAnalyticsRecoverySyncCommandValidator _sut = new RunAnalyticsRecoverySyncCommandValidator();

    #region ========== *** Validate_WithInterval?? *** ==========

    [Fact]
    public void Validate_WithIntervalOnly_AnalyticsCategory_ShouldNotHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(101L,
                                                    SyncAnalyticsCategory.UsersDetails,
                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                    null,
                                                    null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithInvalidIntervalFormat_ShouldHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(101L,
                                                    SyncAnalyticsCategory.UsersDetails,
                                                    "invalid-interval",
                                                    null,
                                                    null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval)
              .WithErrorMessage("Interval format is invalid. Expected UTC interval: yyyy-MM-ddTHH:mmZ/yyyy-MM-ddTHH:mmZ.");
    }

    [Fact]
    public void Validate_WithIntervalLongerThan50_ShouldHaveValidationError()
    {
        string longInterval = new string('I', 51);

        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(101L,
                                                    SyncAnalyticsCategory.UsersDetails,
                                                    longInterval,
                                                    null,
                                                    null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval)
              .WithErrorMessage("Interval cannot exceed 50 characters.");
    }

    [Fact]
    public void Validate_WithIntervalAndGenesysJobId_ShouldHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(101L,
                                                    SyncAnalyticsCategory.ConversationsDetails,
                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                    null,
                                                    "JOB-123");

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Provide either Interval or GenesysJobId, not both.");
    }

    [Fact]
    public void Validate_WithMissingIntervalAndGenesysJobId_ShouldHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(101L,
                                                    SyncAnalyticsCategory.UsersDetails,
                                                    null,
                                                    null,
                                                    null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Either Interval or GenesysJobId must be provided.");
    }

    #endregion

    #region ========== *** Validate_WithGenesysJob?? *** ==========

    [Fact]
    public void Validate_WithGenesysJobIdForUsersDetails_ShouldHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(101L,
                                                    SyncAnalyticsCategory.UsersDetails,
                                                    null,
                                                    null,
                                                    "JOB-123");

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("GenesysJobId is only supported for ConversationsDetails recovery.");
    }

    [Fact]
    public void Validate_WithGenesysJobIdForConversationsDetails_ShouldNotHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(101L,
                                                    SyncAnalyticsCategory.ConversationsDetails,
                                                    null,
                                                    null,
                                                    "JOB-123");

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    [Fact]
    public void Validate_WithRequestIdLessThan1_ShouldHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(0L,
                                                    SyncAnalyticsCategory.UsersDetails,
                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                    null,
                                                    null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RequestId)
              .WithErrorMessage("RequestId must be greater than zero.");
    }

    [Fact]
    public void Validate_WithPageNumberLessThan1_ShouldHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(101L,
                                                    SyncAnalyticsCategory.UsersDetails,
                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                    0,
                                                    null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
              .WithErrorMessage("PageNumber must be greater than or equal to 1 when provided.");
    }

    [Fact]
    public void Validate_InvalidCategory_ShouldHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(101L,
                                                    (SyncAnalyticsCategory)(-1),
                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                    null,
                                                    null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Category)
              .WithErrorMessage("Category is invalid.");
    }

    [Theory]
    [InlineData(SyncAnalyticsCategory.UsersDetails)]
    [InlineData(SyncAnalyticsCategory.ConversationsDetails)]
    [InlineData(SyncAnalyticsCategory.ConversationsAggregates)]
    public void Validate_EachAnalyticsCategory_WithIntervalOnly_ShouldNotHaveValidationError(
            SyncAnalyticsCategory category)
    {
        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(101L,
                                                    category,
                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                    null,
                                                    null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
