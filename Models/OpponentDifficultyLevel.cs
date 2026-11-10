using System.Text.Json.Serialization;

namespace CrisGameRoom.Models;

public sealed class OpponentDifficultyLevel
{
    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonIgnore]
    public string DisplayName => $"{Level}. {Name} — {Count} adversari disponibili";
}
