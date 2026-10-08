using System.Text.Json.Serialization;

namespace CrisGameRoom.Models;

public sealed class UpdateInfo
{
    [JsonPropertyName("product")]
    public string Product { get; set; } = "";

    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("mandatory")]
    public bool Mandatory { get; set; }

    [JsonPropertyName("releaseNotes")]
    public string ReleaseNotes { get; set; } = "";

    [JsonPropertyName("x64Url")]
    public string X64Url { get; set; } = "";

    [JsonPropertyName("x86Url")]
    public string X86Url { get; set; } = "";

    [JsonPropertyName("x64Sha256")]
    public string X64Sha256 { get; set; } = "";

    [JsonPropertyName("x86Sha256")]
    public string X86Sha256 { get; set; } = "";

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset PublishedAt { get; set; }
}
