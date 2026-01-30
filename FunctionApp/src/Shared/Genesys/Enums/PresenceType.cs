using System.Text.Json.Serialization;


namespace Shared.Genesys.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PresenceType
{
    System,
    User
}
