using Application.Features.Shared;

using Xunit;


namespace tests.Unit.Application.Features.Shared;

public sealed class RecoveryValidationRulesTests
{
    [Theory]
    [InlineData(null, false, true)]
    [InlineData("", false, true)]
    [InlineData("   ", false, true)]
    [InlineData("JOB-123", true, true)]
    [InlineData("JOB-123", false, false)]
    public void OnlyUseGenesysJobIdForConversationsDetails_ReturnsExpectedResult(string? genesysJobId,
        bool isConversationsDetailsCategory,
        bool expected)
    {
        bool actual =
                        RecoveryValidationRules.OnlyUseGenesysJobIdForConversationsDetails(genesysJobId,
                            isConversationsDetailsCategory);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("JOB-123", true)]
    [InlineData(" JOB-123", false)]
    [InlineData("JOB-123 ", false)]
    [InlineData(" JOB-123 ", false)]
    public void NotHaveLeadingOrTrailingSpaces_ReturnsExpectedResult(string? genesysJobId, bool expected)
    {
        bool actual = RecoveryValidationRules.NotHaveLeadingOrTrailingSpaces(genesysJobId);

        Assert.Equal(expected, actual);
    }
}
