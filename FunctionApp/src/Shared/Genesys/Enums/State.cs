using System.Text.Json.Serialization;


namespace Shared.Genesys.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum State
{
    Active,
    Inactive,
    Deleted
}
