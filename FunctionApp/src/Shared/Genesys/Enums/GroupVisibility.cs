using System.Text.Json.Serialization;


namespace Shared.Genesys.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GroupVisibility
{
    Public,
    Owners,
    Members
}
