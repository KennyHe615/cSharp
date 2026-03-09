using System.Diagnostics.CodeAnalysis;

using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Features.SyncTracking;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class SyncRunCoordinatorTests
{
    [Fact]
    public async Task StartNewRunAsync_DelegatesToRepository()
    {
        StubSyncRunRepository repo = new StubSyncRunRepository { NextRunId = 88L };
        SyncRunCoordinator sut = new SyncRunCoordinator(repo);

        long runId = await sut.StartNewRunAsync(11L, CancellationToken.None);

        Assert.Equal(88L, runId);
        Assert.Equal(11L, repo.LastRequestId);
        Assert.Equal(1, repo.StartCalls);
    }

    [Fact]
    public async Task IsCurrentRunAsync_DelegatesToRepository()
    {
        StubSyncRunRepository repo = new StubSyncRunRepository { NextIsCurrent = true };
        SyncRunCoordinator sut = new SyncRunCoordinator(repo);

        bool actual = await sut.IsCurrentRunAsync(22L, CancellationToken.None);

        Assert.True(actual);
        Assert.Equal(22L, repo.LastRunId);
        Assert.Equal(1, repo.IsCurrentCalls);
    }

    [Fact]
    public async Task MarkCompletedAsync_DelegatesToRepository()
    {
        StubSyncRunRepository repo = new StubSyncRunRepository();
        SyncRunCoordinator sut = new SyncRunCoordinator(repo);

        await sut.MarkCompletedAsync(33L, CancellationToken.None);

        Assert.Equal(33L, repo.LastRunId);
        Assert.Equal(1, repo.MarkCompletedCalls);
    }

    [Fact]
    public async Task MarkFailedAsync_DelegatesToRepository()
    {
        StubSyncRunRepository repo = new StubSyncRunRepository();
        SyncRunCoordinator sut = new SyncRunCoordinator(repo);

        await sut.MarkFailedAsync(44L, "boom", CancellationToken.None);

        Assert.Equal(44L, repo.LastRunId);
        Assert.Equal("boom", repo.LastReason);
        Assert.Equal(1, repo.MarkFailedCalls);
    }

    [Fact]
    public async Task MarkSupersededAsync_DelegatesToRepository()
    {
        StubSyncRunRepository repo = new StubSyncRunRepository();
        SyncRunCoordinator sut = new SyncRunCoordinator(repo);

        await sut.MarkSupersededAsync(55L, 66L, CancellationToken.None);

        Assert.Equal(55L, repo.LastRunId);
        Assert.Equal(66L, repo.LastSupersededByRunId);
        Assert.Equal(1, repo.MarkSupersededCalls);
    }

    [Fact]
    public async Task MarkCanceledAsync_DelegatesToRepository()
    {
        StubSyncRunRepository repo = new StubSyncRunRepository();
        SyncRunCoordinator sut = new SyncRunCoordinator(repo);

        await sut.MarkCanceledAsync(77L, "cancel", CancellationToken.None);

        Assert.Equal(77L, repo.LastRunId);
        Assert.Equal("cancel", repo.LastReason);
        Assert.Equal(1, repo.MarkCanceledCalls);
    }

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private sealed class StubSyncRunRepository : ISyncRunRepository
    {
        public long NextRunId { get; set; } = 1L;

        public bool NextIsCurrent { get; set; }

        public int StartCalls { get; private set; }

        public int IsCurrentCalls { get; private set; }

        public int MarkCompletedCalls { get; private set; }

        public int MarkFailedCalls { get; private set; }

        public int MarkSupersededCalls { get; private set; }

        public int MarkCanceledCalls { get; private set; }

        public long? LastRequestId { get; private set; }

        public long? LastRunId { get; private set; }

        public long? LastSupersededByRunId { get; private set; }

        public string? LastReason { get; private set; }

        public Task<long> StartNewRunAsync(long requestId, CancellationToken ct)
        {
            StartCalls++;
            LastRequestId = requestId;

            return Task.FromResult(NextRunId);
        }

        public Task<SyncRunDto?> GetByIdAsync(long runId, CancellationToken ct)
        {
            LastRunId = runId;

            return Task.FromResult<SyncRunDto?>(null);
        }

        public Task<bool> IsCurrentRunAsync(long runId, CancellationToken ct)
        {
            IsCurrentCalls++;
            LastRunId = runId;

            return Task.FromResult(NextIsCurrent);
        }

        public Task MarkCompletedAsync(long runId, CancellationToken ct)
        {
            MarkCompletedCalls++;
            LastRunId = runId;

            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(long runId, string reason, CancellationToken ct)
        {
            MarkFailedCalls++;
            LastRunId = runId;
            LastReason = reason;

            return Task.CompletedTask;
        }

        public Task MarkSupersededAsync(long runId, long supersededByRunId, CancellationToken ct)
        {
            MarkSupersededCalls++;
            LastRunId = runId;
            LastSupersededByRunId = supersededByRunId;

            return Task.CompletedTask;
        }

        public Task MarkCanceledAsync(long runId, string? reason, CancellationToken ct)
        {
            MarkCanceledCalls++;
            LastRunId = runId;
            LastReason = reason;

            return Task.CompletedTask;
        }
    }

    #endregion
}
