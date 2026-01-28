using System.Text.Json.Serialization;


namespace Infrastructure.Genesys.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PresenceType
{
    System,
    User
}
