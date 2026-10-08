using System.IO;
using System.Text.Json;
using CrisGameRoom.Models;

namespace CrisGameRoom.Services.Authentication;

public sealed class SessionStore
{
    private readonly string _directory =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CrisGameRoom");

    private string FilePath => Path.Combine(_directory, "session.json");

    public async Task SaveAsync(AuthResult result)
    {
        if (string.IsNullOrWhiteSpace(result.AccessToken))
            throw new InvalidOperationException("Serverul nu a returnat tokenul de autentificare.");

        Directory.CreateDirectory(_directory);

        var payload = new StoredSession
        {
            AccessToken = result.AccessToken,
            ExpiresAt = result.ExpiresAt
        };

        var json = JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(FilePath, json);
    }

    public async Task<StoredSession?> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(FilePath);
            var session = JsonSerializer.Deserialize<StoredSession>(json);

            if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
                return null;

            if (session.ExpiresAt.HasValue &&
                session.ExpiresAt.Value <= DateTimeOffset.UtcNow)
            {
                await ClearAsync();
                return null;
            }

            return session;
        }
        catch
        {
            await ClearAsync();
            return null;
        }
    }

    public Task ClearAsync()
    {
        try
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
        catch
        {
        }

        return Task.CompletedTask;
    }
}

public sealed class StoredSession
{
    public string AccessToken { get; set; } = "";
    public DateTimeOffset? ExpiresAt { get; set; }
}
