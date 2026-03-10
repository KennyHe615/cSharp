using Application.Enums;
using Application.Features.SyncTracking;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class RunRecoverySyncCommandTests
{
    [Fact]
    public void Constructor_ShouldAssignAllProperties()
    {
        RunRecoverySyncCommand command = new RunRecoverySyncCommand(SyncCategory.UsersDetails,
                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                    2,
                                                                    "JOB-123");

        Assert.Equal(SyncCategory.UsersDetails, command.Category);
        Assert.Equal("2026-01-01T00:00Z/2026-01-01T00:30Z", command.Interval);
        Assert.Equal(2, command.PageNumber);
        Assert.Equal("JOB-123", command.GenesysJobId);
    }

    [Fact]
    public void ValueEquality_SameValues_ShouldBeEqual()
    {
        RunRecoverySyncCommand left = new RunRecoverySyncCommand(SyncCategory.ConversationsDetails,
                                                                 null,
                                                                 null,
                                                                 "JOB-999");
        RunRecoverySyncCommand right = new RunRecoverySyncCommand(SyncCategory.ConversationsDetails,
                                                                  null,
                                                                  null,
                                                                  "JOB-999");

        Assert.Equal(left, right);
    }

    [Fact]
    public void ValueEquality_DifferentValues_ShouldNotBeEqual()
    {
        RunRecoverySyncCommand left = new RunRecoverySyncCommand(SyncCategory.ConversationsDetails,
                                                                 null,
                                                                 null,
                                                                 "JOB-999");
        RunRecoverySyncCommand right = new RunRecoverySyncCommand(SyncCategory.ConversationsDetails,
                                                                  "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                  null,
                                                                  null);

        Assert.NotEqual(left, right);
    }
}
