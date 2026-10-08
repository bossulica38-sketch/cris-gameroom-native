using System.Text.Json.Serialization;

namespace CrisGameRoom.Models;

public sealed class AuthResult
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [JsonPropertyName("user")]
    public PublicUser? User { get; set; }

    [JsonPropertyName("emailVerificationRequired")]
    public bool? EmailVerificationRequired { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
