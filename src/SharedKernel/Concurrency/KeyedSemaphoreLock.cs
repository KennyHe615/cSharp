using System.Collections.Concurrent;


namespace SharedKernel.Concurrency;

/// <summary>
/// Provides async keyed mutual exclusion using <see cref="SemaphoreSlim"/> per key.
/// </summary>
/// <remarks>
/// Each unique key has an independent semaphore. Entries are reference-counted and
/// removed/disposed when no waiter/holder remains.
/// </remarks>
public sealed class KeyedSemaphoreLock
{
    private readonly ConcurrentDictionary<string, RefCountedSemaphore> _locks =
        new ConcurrentDictionary<string, RefCountedSemaphore>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Acquires an async lock for the specified key.
    /// </summary>
    /// <param name="key">Logical key used to isolate lock contention.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// An <see cref="IAsyncDisposable"/> releaser that must be disposed to release the lock.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null/empty/whitespace.</exception>
    public async ValueTask<IAsyncDisposable> AcquireAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Lock key cannot be null, empty, or whitespace.", nameof(key));
        }

        RefCountedSemaphore entry = _locks.AddOrUpdate(key,
                                                       static _ => new RefCountedSemaphore(),
                                                       static (_, existing) =>
                                                       {
                                                           existing.AddRef();

                                                           return existing;
                                                       });

        try
        {
            await entry.Semaphore.WaitAsync(ct).ConfigureAwait(false);

            return new Releaser(this, key, entry);
        }
        catch
        {
            ReleaseRef(key, entry);

            throw;
        }
    }

    #region ========== *** Private Methods *** ==========

    private void Release(string key, RefCountedSemaphore entry)
    {
        entry.Semaphore.Release();
        ReleaseRef(key, entry);
    }

    private void ReleaseRef(string key, RefCountedSemaphore entry)
    {
        if (entry.ReleaseRef() != 0) return;

        if (_locks.TryGetValue(key, out RefCountedSemaphore? current)
            && ReferenceEquals(current, entry)
            && _locks.TryRemove(key, out RefCountedSemaphore? removed))
        {
            removed.Dispose();
        }
    }

    #endregion

    #region ========== *** Private Classes *** ==========

    private sealed class Releaser(KeyedSemaphoreLock owner,
                                  string key,
                                  RefCountedSemaphore entry) : IAsyncDisposable
    {
        private int _disposed;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return ValueTask.CompletedTask;

            owner.Release(key, entry);

            return ValueTask.CompletedTask;
        }
    }

    private sealed class RefCountedSemaphore : IDisposable
    {
        private int _refCount = 1;

        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);

        public void AddRef()
        {
            Interlocked.Increment(ref _refCount);
        }

        public int ReleaseRef()
        {
            return Interlocked.Decrement(ref _refCount);
        }

        public void Dispose()
        {
            Semaphore.Dispose();
        }
    }

    #endregion
}
