using System.Text.Json.Serialization;


namespace Infrastructure.Genesys.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SystemPresence
{
    Available,
    Away,
    Busy,
    Offline,
    Idle,
    OnQueue,
    Meal,
    Training,
    Meeting,
    Break
}
