namespace Application.Shared.Context;

public interface ILobContext
{
    string LobName { get; }

    public string GenesysClientId { get; }

    public string GenesysClientSecret { get; }

    public string DatabaseConnectionString { get; }
}

public interface ILobContextAccessor
{
    string? LobName { get; set; }
}
