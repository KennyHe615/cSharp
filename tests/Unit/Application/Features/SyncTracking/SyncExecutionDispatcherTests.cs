using System.Diagnostics.CodeAnalysis;

using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.SyncTracking;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class SyncExecutionDispatcherTests
{
    [Fact]
    public async Task ExecuteAsync_SupportedRoute_WritesRunningThenCompletedCheckpoint()
    {
        StubCheckpointRepository checkpoints = new StubCheckpointRepository();
        SyncExecutionDispatcher sut = new SyncExecutionDispatcher(checkpoints);

        await sut.ExecuteAsync(10L,
                               SyncCategory.UsersDetails,
                               SyncMode.Incremental,
                               "2026-03-10T00:00Z/2026-03-10T00:30Z",
                               null,
                               null,
                               CancellationToken.None);

        Assert.Equal(2, checkpoints.UpsertCalls.Count);

        Assert.Equal(SyncRunStatus.Running, checkpoints.UpsertCalls[0].Status);
        Assert.Equal("Dispatch", checkpoints.UpsertCalls[0].Step);

        Assert.Equal(SyncRunStatus.Completed, checkpoints.UpsertCalls[1].Status);
        Assert.Equal("Dispatch", checkpoints.UpsertCalls[1].Step);

        Assert.Equal(checkpoints.UpsertCalls[0].Cursor, checkpoints.UpsertCalls[1].Cursor);
    }

    [Fact]
    public async Task ExecuteAsync_ReferenceRecovery_ThrowsAndWritesFailedCheckpoint()
    {
        StubCheckpointRepository checkpoints = new StubCheckpointRepository();
        SyncExecutionDispatcher sut = new SyncExecutionDispatcher(checkpoints);

        NotSupportedException ex =
            await Assert.ThrowsAsync<NotSupportedException>(() => sut.ExecuteAsync(20L,
                                                             SyncCategory.User,
                                                             SyncMode.Recovery,
                                                             null,
                                                             null,
                                                             "JOB-1",
                                                             CancellationToken.None));

        Assert.Contains("Recovery mode is not supported for References categories.", ex.Message);
        Assert.Equal(2, checkpoints.UpsertCalls.Count);
        Assert.Equal(SyncRunStatus.Running, checkpoints.UpsertCalls[0].Status);
        Assert.Equal(SyncRunStatus.Failed, checkpoints.UpsertCalls[1].Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCheckpointWriteThrowsOperationCanceled_WritesCanceledAndRethrows()
    {
        StubCheckpointRepository checkpoints = new StubCheckpointRepository
                                               {
                                                   ThrowWhenStatus =
                                                       SyncRunStatus.Completed,
                                                   ExceptionToThrow =
                                                       new
                                                           OperationCanceledException("cancelled")
                                               };
        SyncExecutionDispatcher sut = new SyncExecutionDispatcher(checkpoints);

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.ExecuteAsync(30L,
                                                              SyncCategory.UsersDetails,
                                                              SyncMode.Incremental,
                                                              null,
                                                              null,
                                                              null,
                                                              CancellationToken.None));

        Assert.Equal(3, checkpoints.UpsertCalls.Count);
        Assert.Equal(SyncRunStatus.Running, checkpoints.UpsertCalls[0].Status);
        Assert.Equal(SyncRunStatus.Completed, checkpoints.UpsertCalls[1].Status);
        Assert.Equal(SyncRunStatus.Canceled, checkpoints.UpsertCalls[2].Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCheckpointWriteThrowsNonCancel_WritesFailedAndRethrows()
    {
        StubCheckpointRepository checkpoints = new StubCheckpointRepository
                                               {
                                                   ThrowWhenStatus =
                                                       SyncRunStatus.Completed,
                                                   ExceptionToThrow =
                                                       new
                                                           InvalidOperationException("checkpoint-fail")
                                               };
        SyncExecutionDispatcher sut = new SyncExecutionDispatcher(checkpoints);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(40L,
                                                                 SyncCategory.UsersDetails,
                                                                 SyncMode.Incremental,
                                                                 null,
                                                                 null,
                                                                 null,
                                                                 CancellationToken.None));

        Assert.Equal("checkpoint-fail", ex.Message);
        Assert.Equal(3, checkpoints.UpsertCalls.Count);
        Assert.Equal(SyncRunStatus.Running, checkpoints.UpsertCalls[0].Status);
        Assert.Equal(SyncRunStatus.Completed, checkpoints.UpsertCalls[1].Status);
        Assert.Equal(SyncRunStatus.Failed, checkpoints.UpsertCalls[2].Status);
    }

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private sealed class StubCheckpointRepository : ISyncCheckpointRepository
    {
        public List<UpsertCall> UpsertCalls { get; } = [];

        public SyncRunStatus? ThrowWhenStatus { get; init; }

        public Exception? ExceptionToThrow { get; init; }

        public Task UpsertAsync(long runId,
                                string step,
                                string cursor,
                                SyncRunStatus status,
                                string? failureReason,
                                CancellationToken ct)
        {
            UpsertCalls.Add(new UpsertCall
                            {
                                RunId = runId,
                                Step = step,
                                Cursor = cursor,
                                Status = status,
                                FailureReason = failureReason
                            });

            if (ThrowWhenStatus == status && ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }

        public Task<SyncCheckpointDto?> GetLatestCompletedAsync(long runId, string step, CancellationToken ct)
        {
            return Task.FromResult<SyncCheckpointDto?>(null);
        }

        public Task<IReadOnlyCollection<SyncCheckpointDto>> GetFailedAsync(long runId, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyCollection<SyncCheckpointDto>>(Array.Empty<SyncCheckpointDto>());
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class UpsertCall
    {
        public long RunId { get; set; }

        public string Step { get; init; } = string.Empty;

        public string Cursor { get; init; } = string.Empty;

        public SyncRunStatus Status { get; init; }

        public string? FailureReason { get; set; }
    }

    #endregion
}
