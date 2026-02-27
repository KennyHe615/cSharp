namespace SharedKernel.Logging;

public static class LobLogTemplates
{
    public const string Lob = "[🥇{LobName}] ";
    public const string LobCategory = "[🥇{LobName}🥈{CategoryName}] ";
    public const string LobEntity = "[🥇{LobName}🥉{EntityName}] ";
    public const string LobCategoryEntity = "[🥇{LobName}🥈{CategoryName}🥉{EntityName}] ";
}
