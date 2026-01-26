using FunctionApp.Configuration.Options;


namespace FunctionApp.Application.Shared.Context;

public interface ILobContext
{
    string? LobName { get; set; }

    LobSettings? LobSettings { get; set; }
}
