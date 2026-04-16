using Application.Abstractions.Context;


namespace tests.TestSupport.Context;

public sealed class StubLobContextAccessor : ILobContextAccessor
{
    public string? LobName { get; set; }

    public string? GenesysClientId { get; set; }

    public string? GenesysClientSecret { get; set; }

    public string? DbConnectionString { get; set; }
}
