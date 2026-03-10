using System.Diagnostics.CodeAnalysis;

using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.SyncTracking;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class RunRecoverySyncCommandHandlerTests
{
    [Fact]
    public async Task Handle_AnalyticsCategory_CreatesRecoveryScope_ExecutesAndReturnsRequestId()
    {
        StubSyncRequestRepository requestRepository = new StubSyncRequestRepository { NextRequestId = 101L };
        StubSyncRequestRunner runner = new StubSyncRequestRunner();
        RunRecoverySyncCommandHandler sut = new RunRecoverySyncCommandHandler(requestRepository, runner);
        RunRecoverySyncCommand command = new RunRecoverySyncCommand(SyncCategory.UsersDetails,
                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                    2,
                                                                    "JOB-123");

        long result = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(101L, result);
        Assert.Single(requestRepository.CreateCalls);
        Assert.Equal(SyncCategory.UsersDetails, requestRepository.CreateCalls[0].Category);
        Assert.Equal(SyncMode.Recovery, requestRepository.CreateCalls[0].Mode);
        Assert.Equal("2026-01-01T00:00Z/2026-01-01T00:30Z", requestRepository.CreateCalls[0].Interval);
        Assert.Equal(2, requestRepository.CreateCalls[0].PageNumber);
        Assert.Equal("JOB-123", requestRepository.CreateCalls[0].GenesysJobId);
        Assert.Equal(1, runner.ExecuteCalls);
        Assert.Equal(101L, runner.LastRequestId);
    }

    [Fact]
    public async Task Handle_ReferencesCategory_ThrowsInvalidOperationException_WithoutRepositoryCall()
    {
        StubSyncRequestRepository requestRepository = new StubSyncRequestRepository();
        StubSyncRequestRunner runner = new StubSyncRequestRunner();

        RunRecoverySyncCommandHandler sut = new RunRecoverySyncCommandHandler(requestRepository, runner);
        RunRecoverySyncCommand command =
            new RunRecoverySyncCommand(SyncCategory.Queue,
                                       "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                       null,
                                       null);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Contains("Recovery mode is not supported for category", ex.Message);
        Assert.Empty(requestRepository.CreateCalls);
        Assert.Equal(0, runner.ExecuteCalls);
    }

    [Fact]
    public async Task Handle_RunExecutionThrows_RethrowsSameException()
    {
        StubSyncRequestRepository requestRepository = new StubSyncRequestRepository
                                                      {
                                                          NextRequestId = 201L
                                                      };
        InvalidOperationException original = new InvalidOperationException("run failed");
        StubSyncRequestRunner runner = new StubSyncRequestRunner
                                       {
                                           ThrowOnExecute = original
                                       };

        RunRecoverySyncCommandHandler sut = new RunRecoverySyncCommandHandler(requestRepository, runner);
        RunRecoverySyncCommand command = new RunRecoverySyncCommand(SyncCategory.ConversationsDetails,
                                                                    null,
                                                                    null,
                                                                    "JOB-999");

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Same(original, ex);
        Assert.Single(requestRepository.CreateCalls);
        Assert.Equal(1, runner.ExecuteCalls);
    }

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private sealed class StubSyncRequestRunner : ISyncRequestRunner
    {
        public int ExecuteCalls { get; private set; }

        public long LastRequestId { get; private set; }

        public Exception? ThrowOnExecute { get; init; }

        public Task ExecuteAsync(long requestId, CancellationToken ct)
        {
            ExecuteCalls++;
            LastRequestId = requestId;

            return ThrowOnExecute is not null ? throw ThrowOnExecute : Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubSyncRequestRepository : ISyncRequestRepository
    {
        public long NextRequestId { get; init; } = 1L;

        public List<CreateCall> CreateCalls { get; } = [];

        public Task<long> CreateOrGetByScopeAsync(SyncCategory category,
                                                  SyncMode mode,
                                                  string? interval,
                                                  int? pageNumber,
                                                  string? genesysJobId,
                                                  CancellationToken ct)
        {
            CreateCalls.Add(new CreateCall(category,
                                           mode,
                                           interval,
                                           pageNumber,
                                           genesysJobId));

            return Task.FromResult(NextRequestId);
        }

        public Task<SyncRequestDto?> GetByIdAsync(long id, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task SetCurrentRunAsync(long requestId, long runId, CancellationToken ct)
        {
            throw new NotSupportedException();
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record CreateCall(SyncCategory Category,
                                     SyncMode Mode,
                                     string? Interval,
                                     int? PageNumber,
                                     string? GenesysJobId);

    #endregion
}
