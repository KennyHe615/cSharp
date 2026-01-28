using System.Text.Json.Serialization;


namespace Infrastructure.Genesys.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GroupVisibility
{
    Public,
    Owners,
    Members
}
