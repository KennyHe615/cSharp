using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Contracts.InternalApis.Recovery;
using Application.Features.Recovery;
using Application.Mediator;

using FunctionApp.Http;

using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using tests.TestSupport.Functions;
using tests.TestSupport.Logging;


namespace Tests.Integration.FunctionApp;

[ExcludeFromCodeCoverage]
internal static class RecoveryFunctionHttpTestFixture
{
    internal static RecoveryFunction CreateSut(StubMediator mediator, ILogger<RecoveryFunction>? logger = null)
    {
        StubLobContextAccessor accessor = new StubLobContextAccessor();
        StubCredentialProvider credentialProvider = new StubCredentialProvider();

        return new RecoveryFunction(mediator,
                                    accessor,
                                    credentialProvider,
                                    logger ?? new TestLogger<RecoveryFunction>());
    }

    internal static FakeHttpRequestData CreateRequest(string json)
    {
        FakeFunctionContext context = new FakeFunctionContext();

        return new FakeHttpRequestData(context,
                                       "POST",
                                       "http://localhost/api/recovery",
                                       json);
    }

    internal static string ReadError(HttpResponseData response)
    {
        string body = response.ReadBodyAsString();
        using JsonDocument doc = JsonDocument.Parse(body);

        return doc.RootElement.GetProperty("Error")
                  .GetString()
               ?? string.Empty;
    }

    internal sealed class StubMediator : ISimpleMediator
    {
        internal int SendCount { get; private set; }

        internal CreateRecoveryRequestCommand? LastCommand { get; private set; }

        internal Func<CreateRecoveryRequestCommand, CancellationToken, Task<CreateRecoveryRequestResponse>> OnSend
        {
            get;
            init;
        } = (_, _) => Task.FromResult(new CreateRecoveryRequestResponse(true, "ok", new {}));

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        {
            SendCount++;

            if (request is not CreateRecoveryRequestCommand command)
            {
                throw new InvalidOperationException("Unexpected request type.");
            }

            LastCommand = command;

            CreateRecoveryRequestResponse response = await OnSend(command, ct)
               .ConfigureAwait(false);

            return (TResponse)(object)response;
        }
    }

    private sealed class StubLobContextAccessor : ILobContextAccessor
    {
        public string? LobName { get; set; }

        public string? GenesysClientId { get; set; }

        public string? GenesysClientSecret { get; set; }

        public string? DbConnectionString { get; set; }
    }

    private sealed class StubCredentialProvider : ICredentialProvider
    {
        public Task PopulateAsync(ILobContextAccessor accessor, CancellationToken ct = default)
        {
            accessor.GenesysClientId = "id";
            accessor.GenesysClientSecret = "secret";
            accessor.DbConnectionString = "conn";

            return Task.CompletedTask;
        }
    }

    internal sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly List<LogEntry> _entries = [];

        internal IReadOnlyList<LogEntry> Entries => _entries;

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NoopDisposable.Instance;
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
            _entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }

        internal sealed record LogEntry(LogLevel Level,
                                        string Message,
                                        Exception? Exception);

        private sealed class NoopDisposable : IDisposable
        {
            internal static readonly NoopDisposable Instance = new NoopDisposable();

            public void Dispose()
            {
            }
        }
    }
}
