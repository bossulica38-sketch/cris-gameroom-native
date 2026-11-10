using System.Text.Json.Serialization;
namespace CrisGameRoom.Models;
public sealed class Game
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("slug")] public string Slug { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("mode")] public string Mode { get; set; } = "";
    [JsonPropertyName("lifecycle")] public string Lifecycle { get; set; } = "";
    [JsonPropertyName("version")] public string Version { get; set; } = "";
}
