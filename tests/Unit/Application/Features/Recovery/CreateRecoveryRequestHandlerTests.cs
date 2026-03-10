using System.Diagnostics.CodeAnalysis;

using Application.Abstractions.Persistence;
using Application.Contracts.InternalApis.Recovery;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.Recovery;

using SharedKernel.Lobs;
using SharedKernel.Time;

using Xunit;


namespace tests.Unit.Application.Features.Recovery;

public sealed class CreateRecoveryRequestHandlerTests
{
    [Theory]
    [InlineData(RecoveryCategory.UsersDetails, SyncCategory.UsersDetails)]
    [InlineData(RecoveryCategory.ConversationsDetails, SyncCategory.ConversationsDetails)]
    [InlineData(RecoveryCategory.ConversationsAggregates, SyncCategory.ConversationsAggregates)]
    public async Task Handle_MapsCategoryAndCreatesRequest(RecoveryCategory category, SyncCategory expectedCategory)
    {
        StubSyncRequestRepository repository = new StubSyncRequestRepository { NextCreatedId = 101L };
        CreateRecoveryRequestHandler sut = new CreateRecoveryRequestHandler(repository);

        UtcInterval interval = new UtcInterval(new DateTimeOffset(2025,
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

        CreateRecoveryRequestCommand command = new CreateRecoveryRequestCommand(new LobName("CRC"),
                                                                                category,
                                                                                interval,
                                                                                "JOB-123");

        CreateRecoveryRequestResponse response = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(1, repository.CreateOrGetCalls);
        Assert.Equal(expectedCategory, repository.LastCategory);
        Assert.Equal(SyncMode.Recovery, repository.LastMode);
        Assert.Equal(interval.ToString(), repository.LastInterval);
        Assert.Null(repository.LastPageNumber);
        Assert.Equal("JOB-123", repository.LastGenesysJobId);

        Assert.True(response.Success);
        Assert.Equal("Recovery request created successfully.", response.Message);

        object detail = response.Data;
        Assert.NotNull(detail);

        Type detailType = detail.GetType();
        Assert.Equal(101L, (long)detailType.GetProperty("Id")!.GetValue(detail)!);
        Assert.Equal("CRC", (string)detailType.GetProperty("Lob")!.GetValue(detail)!);
        Assert.Equal(category.ToString(), (string)detailType.GetProperty("Category")!.GetValue(detail)!);
        Assert.Equal(interval, (UtcInterval?)detailType.GetProperty("Interval")!.GetValue(detail)!);
        Assert.Equal("JOB-123", (string?)detailType.GetProperty("GenesysJobId")!.GetValue(detail)!);
    }

    [Fact]
    public async Task Handle_UnsupportedCategory_ThrowsInvalidOperationException()
    {
        StubSyncRequestRepository repository = new StubSyncRequestRepository();
        CreateRecoveryRequestHandler sut = new CreateRecoveryRequestHandler(repository);

        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("CRC"),
                                             (RecoveryCategory)999,
                                             null,
                                             null);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Contains("Unsupported recovery category", ex.Message);
        Assert.Equal(0, repository.CreateOrGetCalls);
    }

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private sealed class StubSyncRequestRepository : ISyncRequestRepository
    {
        public int CreateOrGetCalls { get; private set; }

        public long NextCreatedId { get; set; } = 1L;

        public SyncCategory? LastCategory { get; private set; }

        public SyncMode? LastMode { get; private set; }

        public string? LastInterval { get; private set; }

        public int? LastPageNumber { get; private set; }

        public string? LastGenesysJobId { get; private set; }

        public Task<long> CreateOrGetByScopeAsync(SyncCategory category,
                                                  SyncMode mode,
                                                  string? interval,
                                                  int? pageNumber,
                                                  string? genesysJobId,
                                                  CancellationToken ct)
        {
            CreateOrGetCalls++;
            LastCategory = category;
            LastMode = mode;
            LastInterval = interval;
            LastPageNumber = pageNumber;
            LastGenesysJobId = genesysJobId;

            return Task.FromResult(NextCreatedId);
        }

        public Task<SyncRequestDto?> GetByIdAsync(long id, CancellationToken ct)
        {
            return Task.FromResult<SyncRequestDto?>(null);
        }

        public Task SetCurrentRunAsync(long requestId, long runId, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

    #endregion
}
