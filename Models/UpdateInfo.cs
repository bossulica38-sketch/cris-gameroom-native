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

    [JsonPropertyName("updaterUrlX64")]
    public string UpdaterUrlX64 { get; set; } = "";

    [JsonPropertyName("updaterUrlX86")]
    public string UpdaterUrlX86 { get; set; } = "";

    [JsonPropertyName("updaterSha256X64")]
    public string UpdaterSha256X64 { get; set; } = "";

    [JsonPropertyName("updaterSha256X86")]
    public string UpdaterSha256X86 { get; set; } = "";

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset PublishedAt { get; set; }
}
