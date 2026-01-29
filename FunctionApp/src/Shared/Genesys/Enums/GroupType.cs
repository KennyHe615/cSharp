using System.Text.Json.Serialization;


namespace Shared.Genesys.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GroupType
{
    Official,
    Social
}
