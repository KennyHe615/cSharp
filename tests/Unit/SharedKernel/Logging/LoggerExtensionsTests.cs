using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

using SharedKernel.Lobs;
using SharedKernel.Logging;

using Xunit;

using SdkLoggerExtensions=SharedKernel.Logging.LoggerExtensions;


namespace tests.Unit.SharedKernel.Logging;

public sealed class LoggerExtensionsTests
{
    #region BeginOperationScope

    [Fact]
    public void BeginOperationScope_PushesLobCategoryEntityIntoScope()
    {
        TestLogger logger = new TestLogger();

        using IDisposable _ = logger.BeginOperationScope(new LobName("NTT"), "References", "Group");

        Assert.Single(logger.Scopes);
        Assert.IsType<Dictionary<string, object?>>(logger.Scopes[0]);

        Dictionary<string, object?> scope = (Dictionary<string, object?>)logger.Scopes[0];
        Assert.Equal("NTT", scope["lob"]);
        Assert.Equal("References", scope["category"]);
        Assert.Equal("Group", scope["entity"]);
        Assert.True(scope.ContainsKey("trace_id"));
        Assert.True(scope.ContainsKey("span_id"));
        Assert.True(scope.ContainsKey("activity_id"));
    }

    [Fact]
    public void BeginOperationScope_UsesNoopScope_WhenLoggerReturnsNullScope()
    {
        TestLogger logger = new TestLogger(returnNullScope: true);

        using IDisposable _ = logger.BeginOperationScope(new LobName("CRC"), "UserDetails");

        Assert.Empty(logger.Scopes);// no BeginScope captured because logger returned null
    }

    [Fact]
    public void BeginOperationScope_Throws_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
                                                 SdkLoggerExtensions.BeginOperationScope(null!,
                                                  new LobName("NTT"),
                                                  "Cat",
                                                  "Ent"));
    }

    [Fact]
    public void BeginOperationScope_NormalizesWhitespaceCategoryAndEntity_ToNull()
    {
        TestLogger logger = new TestLogger();

        using IDisposable _ = logger.BeginOperationScope(new LobName("CRC"), "   ", "\t");

        Dictionary<string, object?> scope = Assert.IsType<Dictionary<string, object?>>(logger.Scopes[0]);
        Assert.Equal("CRC", scope["lob"]);
        Assert.Null(scope["category"]);
        Assert.Null(scope["entity"]);
    }

    [Fact]
    public void BeginOperationScope_UsesExistingActivity_WhenPresent()
    {
        TestLogger logger = new TestLogger();

        using Activity existing = new Activity("existing");
        existing.Start();

        using IDisposable _ = logger.BeginOperationScope(new LobName("LCL"), "Refs", "Group");

        Dictionary<string, object?> scope = Assert.IsType<Dictionary<string, object?>>(logger.Scopes[0]);
        Assert.Equal(existing.TraceId.ToString(), scope["trace_id"]);
        Assert.Equal(existing.SpanId.ToString(), scope["span_id"]);
        Assert.Equal(existing.Id, scope["activity_id"]);
    }

    [Fact]
    public void BeginOperationScope_DisposeTwice_IsSafe_AndPopsScopeOnce()
    {
        TestLogger logger = new TestLogger();

        IDisposable scope = logger.BeginOperationScope(new LobName("NTT"), "Refs", "Group");

        Assert.Single(logger.Scopes);

        scope.Dispose();
        scope.Dispose();// hits ScopePopper _disposed guard

        Assert.Empty(logger.Scopes);
    }

    [Fact]
    public void BeginOperationScope_CreatesCompositeDisposable_WhenNewActivityIsStarted()
    {
        using ActivityTestProbe probe = ActivityTestProbe.Start("SharedKernel.Logging");
        TestLogger logger = new TestLogger();

        IDisposable scope = logger.BeginOperationScope(new LobName("NTT"), "Refs", "Group");
        scope.Dispose();

        Assert.Equal(1, probe.StartedCount);
        Assert.Equal(1, probe.StoppedCount);// proves second disposable (Activity) was disposed
    }

    [Fact]
    public void BeginOperationScope_CompositeDisposable_FinallyDisposesActivity_WhenScopeDisposeThrows()
    {
        using ActivityTestProbe probe = ActivityTestProbe.Start("SharedKernel.Logging");
        ThrowingScopeLogger logger = new ThrowingScopeLogger();

        IDisposable scope = logger.BeginOperationScope(new LobName("CRC"), "Refs", "Entity");

        Assert.Throws<InvalidOperationException>(() => scope.Dispose());
        Assert.Equal(1, probe.StoppedCount);// proves finally block executed
    }

    [Fact]
    public void BeginOperationScope_ReturnsNoopDisposable_WhenExistingActivityAndNullScopeLogger()
    {
        TestLogger logger = new TestLogger(returnNullScope: true);

        using Activity existing = new Activity("existing");
        existing.Start();

        IDisposable scope = logger.BeginOperationScope(new LobName("NTT"), "Refs", "Group");

        // started == null and BeginScope == null => NoopDisposable path
        scope.Dispose();// covers NoopDisposable.Dispose()
    }

    [Fact]
    public void BeginOperationScope_CompositeDisposable_DisposeTwice_IsSafe()
    {
        using ActivityTestProbe probe = ActivityTestProbe.Start("SharedKernel.Logging");
        TestLogger logger = new TestLogger();

        IDisposable scope = logger.BeginOperationScope(new LobName("CRC"), "Refs", "Entity");

        scope.Dispose();
        scope.Dispose();// covers CompositeDisposable _disposed guard

        Assert.Equal(1, probe.StoppedCount);
    }

    #endregion

    #region LogErrorWithDetails

    [Fact]
    public void LogErrorWithDetails_LogsErrorSummaryAndRootCause()
    {
        TestLogger logger = new TestLogger();
        Exception ex = new ApplicationException("outer", new InvalidOperationException("inner-root"));

        logger.LogErrorWithDetails(ex, "Failed for {Lob}", "NTT");

        Assert.True(logger.Entries.Count >= 2);

        LogEntry first = logger.Entries[0];
        Assert.Equal(LogLevel.Error, first.Level);
        Assert.Equal(ex, first.Exception);
        Assert.Contains("Failed for NTT", first.Message);

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("Exception Summary:"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("Root Cause:"));
    }

    [Fact]
    public void LogErrorWithDetails_Works_WhenBeginScopeReturnsNull()
    {
        TestLogger logger = new TestLogger(returnNullScope: true);
        Exception ex = new InvalidOperationException("boom");

        logger.LogErrorWithDetails(ex, "Failed {Lob}", "NTT");

        Assert.NotEmpty(logger.Entries);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("Failed NTT"));
    }

    [Fact]
    public void LogErrorWithDetails_Throws_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
                                                 SdkLoggerExtensions.LogErrorWithDetails(null!,
                                                  new Exception("x"),
                                                  "msg"));
    }

    [Fact]
    public void LogErrorWithDetails_Throws_WhenExceptionIsNull()
    {
        TestLogger logger = new TestLogger();

        Assert.Throws<ArgumentNullException>(() => logger.LogErrorWithDetails(null!, "msg"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void LogErrorWithDetails_Throws_WhenMessageTemplateInvalid(string template)
    {
        TestLogger logger = new TestLogger();

        Assert.Throws<ArgumentException>(() => logger.LogErrorWithDetails(new Exception("x"), template));
    }

    #endregion

    #region LogWarningWithDetails

    [Fact]
    public void LogWarningWithDetails_DoesNotEmitDebugDetails_WhenDebugDisabled()
    {
        TestLogger logger = new TestLogger(enabled: level => level != LogLevel.Debug);
        Exception ex = new InvalidOperationException("boom");

        logger.LogWarningWithDetails(ex, "Warning for {Lob}", "LCL");

        Assert.DoesNotContain(logger.Entries,
                              e => e.Level == LogLevel.Debug && e.Message.Contains("Full Exception Details:"));
    }

    [Fact]
    public void LogWarningWithDetails_LogsDebugDetails_WhenDebugEnabled()
    {
        TestLogger logger = new TestLogger(enabled: _ => true);
        Exception ex = new InvalidOperationException("boom");

        logger.LogWarningWithDetails(ex, "Warning {Lob}", "NTT");

        Assert.Contains(logger.Entries,
                        e => e.Level == LogLevel.Debug && e.Message.Contains("Full Exception Details:"));
    }

    #endregion

    #region LogCriticalWithDetails

    [Fact]
    public void LogCriticalWithDetails_LogsCriticalAndNoRootCauseLine_WhenNoInner()
    {
        TestLogger logger = new TestLogger();
        Exception ex = new InvalidOperationException("single");

        logger.LogCriticalWithDetails(ex, "Critical {Lob}", "CRC");

        Assert.Contains(logger.Entries, e => e.Level       == LogLevel.Critical && e.Message.Contains("Critical CRC"));
        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Critical && e.Message.Contains("Root Cause:"));
    }

    #endregion

    #region ========== *** Private *** ==========

    [ExcludeFromCodeCoverage]
    private sealed class TestLogger(bool returnNullScope = false,
                                    Func<LogLevel, bool>? enabled = null) : ILogger
    {
        private readonly Func<LogLevel, bool> _enabled = enabled ?? (_ => true);

        public List<object> Scopes { get; } = [];

        public List<LogEntry> Entries { get; } = [];

        [ExcludeFromCodeCoverage]
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (returnNullScope) return null;

            Scopes.Add(state);

            return new ScopePopper(Scopes);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _enabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel,
                                EventId eventId,
                                TState state,
                                Exception? exception,
                                Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            Entries.Add(new LogEntry
                        {
                            Level = logLevel,
                            Exception = exception,
                            Message = formatter(state, exception)
                        });
        }

        private sealed class ScopePopper(List<object> scopes) : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed) return;

                _disposed = true;
                if (scopes.Count > 0) scopes.RemoveAt(scopes.Count - 1);
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class LogEntry
    {
        public LogLevel Level { get; init; }

        public Exception? Exception { get; init; }

        public string Message { get; init; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    private sealed class ActivityTestProbe : IDisposable
    {
        private readonly ActivityListener _listener;

        public int StartedCount { get; private set; }

        public int StoppedCount { get; private set; }

        private ActivityTestProbe(string sourceName)
        {
            _listener = new ActivityListener
                        {
                            ShouldListenTo = source => source.Name == sourceName,
                            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                                         ActivitySamplingResult.AllData,
                            ActivityStarted = _ => StartedCount++,
                            ActivityStopped = _ => StoppedCount++
                        };

            ActivitySource.AddActivityListener(_listener);
        }

        public static ActivityTestProbe Start(string sourceName)
        {
            return new ActivityTestProbe(sourceName);
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingScopeLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return new ThrowingDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel,
                                EventId eventId,
                                TState state,
                                Exception? exception,
                                Func<TState, Exception?, string> formatter)
        {
            // no-op for this test logger
        }

        private sealed class ThrowingDisposable : IDisposable
        {
            public void Dispose()
            {
                throw new InvalidOperationException("scope dispose failed");
            }
        }
    }

    #endregion
}
