using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CrisGameRoom.Models;

namespace CrisGameRoom.Services.Authentication;

public sealed class SessionStore
{
    private readonly string _directory =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CrisGameRoom");

    private string FilePath => Path.Combine(_directory, "session.dat");

    public async Task SaveAsync(AuthResult result)
    {
        if (string.IsNullOrWhiteSpace(result.AccessToken))
            throw new InvalidOperationException(
                "Serverul nu a returnat tokenul de autentificare.");

        Directory.CreateDirectory(_directory);

        var payload = new StoredSession
        {
            AccessToken = result.AccessToken,
            ExpiresAt = result.ExpiresAt
        };

        var json = JsonSerializer.Serialize(payload);
        var plaintext = Encoding.UTF8.GetBytes(json);

        var protectedData = ProtectedData.Protect(
            plaintext,
            null,
            DataProtectionScope.CurrentUser);

        await File.WriteAllBytesAsync(FilePath, protectedData);
    }

    public async Task<StoredSession?> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return null;

        try
        {
            var protectedData = await File.ReadAllBytesAsync(FilePath);

            var plaintext = ProtectedData.Unprotect(
                protectedData,
                null,
                DataProtectionScope.CurrentUser);

            var json = Encoding.UTF8.GetString(plaintext);

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

            var legacyFile = Path.Combine(_directory, "session.json");

            if (File.Exists(legacyFile))
                File.Delete(legacyFile);
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
