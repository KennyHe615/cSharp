using SharedKernel.Concurrency;

using Xunit;


namespace tests.Unit.SharedKernel.Concurrency;

public sealed class KeyedSemaphoreLockTests
{
    [Fact]
    public async Task AcquireAsync_SameKey_IsMutuallyExclusive()
    {
        KeyedSemaphoreLock keyedLock = new KeyedSemaphoreLock();
        int active = 0;
        int maxActive = 0;

        await Task.WhenAll(WorkAsync(), WorkAsync(), WorkAsync());

        Assert.Equal(1, maxActive);

        return;

        async Task WorkAsync()
        {
            await using IAsyncDisposable lease = await keyedLock.AcquireAsync("same-key");

            int now = Interlocked.Increment(ref active);
            if (now > maxActive) maxActive = now;

            await Task.Delay(50);

            Interlocked.Decrement(ref active);
        }
    }

    [Fact]
    public async Task AcquireAsync_DifferentKeys_CanRunConcurrently()
    {
        KeyedSemaphoreLock keyedLock = new KeyedSemaphoreLock();
        int active = 0;
        int maxActive = 0;

        async Task WorkAsync(string key)
        {
            await using IAsyncDisposable lease = await keyedLock.AcquireAsync(key);

            int now = Interlocked.Increment(ref active);
            if (now > maxActive) maxActive = now;

            await Task.Delay(50);

            Interlocked.Decrement(ref active);
        }

        await Task.WhenAll(WorkAsync("k1"), WorkAsync("k2"));

        Assert.True(maxActive >= 2);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task AcquireAsync_InvalidKey_Throws(string key)
    {
        KeyedSemaphoreLock keyedLock = new KeyedSemaphoreLock();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
                                                    {
                                                        await using IAsyncDisposable _ =
                                                            await keyedLock.AcquireAsync(key);
                                                    });
    }

    [Fact]
    public async Task AcquireAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        KeyedSemaphoreLock keyedLock = new KeyedSemaphoreLock();

        await using IAsyncDisposable first = await keyedLock.AcquireAsync("cancel-key");

        using CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Exception ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                                                                               {
                                                                                   await using IAsyncDisposable _ =
                                                                                       await keyedLock
                                                                                          .AcquireAsync("cancel-key",
                                                                                            cts.Token);
                                                                               });

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }

    [Fact]
    public async Task AcquireAsync_DisposeTwice_IsSafe()
    {
        KeyedSemaphoreLock keyedLock = new KeyedSemaphoreLock();

        IAsyncDisposable lease = await keyedLock.AcquireAsync("double-dispose");
        await lease.DisposeAsync();
        await lease.DisposeAsync();// covers Releaser's already-disposed branch
    }
}
