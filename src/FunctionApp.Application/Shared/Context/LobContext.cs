using FunctionApp.Configuration.Options;


namespace FunctionApp.Application.Shared.Context;

public class LobContext : ILobContext
{
    public string? LobName { get; set; }

    public LobSettings? LobSettings { get; set; }
}
