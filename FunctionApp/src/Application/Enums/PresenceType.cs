using System.Text.Json.Serialization;


namespace Application.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PresenceType
{
    System,
    User
}
