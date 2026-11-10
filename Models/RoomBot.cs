using System.Text.Json.Serialization;

namespace CrisGameRoom.Models;

public sealed class RoomBot
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("profileId")]
    public string ProfileId { get; set; } = "";

    [JsonPropertyName("legacyId")]
    public int? LegacyId { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("levelName")]
    public string LevelName { get; set; } = "";

    [JsonPropertyName("archetype")]
    public string Archetype { get; set; } = "";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
