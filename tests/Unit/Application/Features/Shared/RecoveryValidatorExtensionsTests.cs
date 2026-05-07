using Application.Features.Shared;

using FluentValidation;
using FluentValidation.TestHelper;

using Xunit;


namespace tests.Unit.Application.Features.Shared;

public sealed class RecoveryValidatorExtensionsTests
{
    private readonly TestRecoveryValidator _sut = new TestRecoveryValidator();

    [Fact]
    public void Validate_WithIntervalOnly_ShouldNotHaveValidationError()
    {
        TestRecoveryRequest request =
                        new TestRecoveryRequest
                        {
                            Interval = "2026-01-01T00:00Z/2026-01-01T00:30Z",
                            GenesysJobId = null,
                            IsConversationsDetails = false
                        };

        TestValidationResult<TestRecoveryRequest> result = _sut.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithGenesysJobIdOnly_ForConversationsDetails_ShouldNotHaveValidationError()
    {
        TestRecoveryRequest request =
                        new TestRecoveryRequest
                        {
                            Interval = null,
                            GenesysJobId = "JOB-123",
                            IsConversationsDetails = true
                        };

        TestValidationResult<TestRecoveryRequest> result = _sut.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMissingIntervalAndGenesysJobId_ShouldHaveValidationError()
    {
        TestRecoveryRequest request =
                        new TestRecoveryRequest
                        {
                            Interval = null,
                            GenesysJobId = null,
                            IsConversationsDetails = false
                        };

        TestValidationResult<TestRecoveryRequest> result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Either Interval or GenesysJobId must be provided.");
    }

    [Fact]
    public void Validate_WithBothIntervalAndGenesysJobId_ShouldHaveValidationError()
    {
        TestRecoveryRequest request =
                        new TestRecoveryRequest
                        {
                            Interval = "2026-01-01T00:00Z/2026-01-01T00:30Z",
                            GenesysJobId = "JOB-123",
                            IsConversationsDetails = true
                        };

        TestValidationResult<TestRecoveryRequest> result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Provide either Interval or GenesysJobId, not both.");
    }

    [Fact]
    public void Validate_WithGenesysJobIdForNonConversationsDetails_ShouldHaveValidationError()
    {
        TestRecoveryRequest request =
                        new TestRecoveryRequest
                        {
                            Interval = null,
                            GenesysJobId = "JOB-123",
                            IsConversationsDetails = false
                        };

        TestValidationResult<TestRecoveryRequest> result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("GenesysJobId is only supported for ConversationsDetails recovery.");
    }

    [Fact]
    public void Validate_WithGenesysJobIdLongerThan100_ShouldHaveValidationError()
    {
        TestRecoveryRequest request =
                        new TestRecoveryRequest
                        {
                            Interval = null,
                            GenesysJobId = new string('A', 101),
                            IsConversationsDetails = true
                        };

        TestValidationResult<TestRecoveryRequest> result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.GenesysJobId)
              .WithErrorMessage("GenesysJobId cannot exceed 100 characters.");
    }

    [Theory]
    [InlineData(" JOB-123")]
    [InlineData("JOB-123 ")]
    public void Validate_WithLeadingOrTrailingSpacesInGenesysJobId_ShouldHaveValidationError(string genesysJobId)
    {
        TestRecoveryRequest request =
                        new TestRecoveryRequest
                        {
                            Interval = null,
                            GenesysJobId = genesysJobId,
                            IsConversationsDetails = true
                        };

        TestValidationResult<TestRecoveryRequest> result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.GenesysJobId)
              .WithErrorMessage("GenesysJobId must not contain leading or trailing spaces.");
    }

    #region ========== *** Private Section *** ==========

    private sealed class TestRecoveryValidator : AbstractValidator<TestRecoveryRequest>
    {
        public TestRecoveryValidator()
        {
            this.AddRecoverySelectorRules(x => !string.IsNullOrWhiteSpace(x.Interval),
                                          x => x.GenesysJobId,
                                          x => x.IsConversationsDetails);
        }
    }

    private sealed class TestRecoveryRequest
    {
        public string? Interval { get; set; }

        public string? GenesysJobId { get; set; }

        public bool IsConversationsDetails { get; set; }
    }

    #endregion
}
