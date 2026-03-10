using Application.Enums;
using Application.Features.SyncTracking;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class RunIncrementalSyncCommandTests
{
    [Fact]
    public void Constructor_ShouldAssignAllProperties()
    {
        RunIncrementalSyncCommand command =
            new RunIncrementalSyncCommand(SyncCategory.UsersDetails, "2026-01-01T00:00Z/2026-01-01T00:30Z", 3);

        Assert.Equal(SyncCategory.UsersDetails, command.Category);
        Assert.Equal("2026-01-01T00:00Z/2026-01-01T00:30Z", command.Interval);
        Assert.Equal(3, command.PageNumber);
    }

    [Fact]
    public void ValueEquality_SameValues_ShouldBeEqual()
    {
        RunIncrementalSyncCommand left = new RunIncrementalSyncCommand(SyncCategory.Queue, null, null);
        RunIncrementalSyncCommand right = new RunIncrementalSyncCommand(SyncCategory.Queue, null, null);

        Assert.Equal(left, right);
    }
}
