using System.Diagnostics.CodeAnalysis;

using Application.Abstractions.Context;

using SharedKernel.Lobs;


namespace tests.TestSupport.Context;

[ExcludeFromCodeCoverage]
public sealed class StubLobContext : ILobContext
{
    public LobName LobName { get; init; } = LobName.Ntt;

    public string GenesysClientId { get; init; } = "client-id";

    public string GenesysClientSecret { get; init; } = "client-secret";

    public string DbConnectionString { get; init; } = "db-connection";
}
