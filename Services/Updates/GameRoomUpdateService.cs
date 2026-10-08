using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CrisGameRoom.Configuration;
using CrisGameRoom.Models;

namespace CrisGameRoom.Services.Updates;

public sealed class GameRoomUpdateService
{
    private const string ManifestUrl =
        "https://criswebhost.ro/downloads/gameroom/stable/latest.json";

    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public Version CurrentVersion =>
        Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0);

    public async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(
            ManifestUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var info = await JsonSerializer.DeserializeAsync<UpdateInfo>(
            stream,
            cancellationToken: cancellationToken);

        if (info is null ||
            !string.Equals(info.Product, ServerConfiguration.ProductName, StringComparison.Ordinal) ||
            !string.Equals(info.Channel, ServerConfiguration.UpdateChannel, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(info.Version))
        {
            return null;
        }

        if (!Version.TryParse(info.Version, out var remoteVersion))
            return null;

        return remoteVersion > CurrentVersion ? info : null;
    }

    public string GetPackageUrl(UpdateInfo info)
    {
        return Environment.Is64BitProcess ? info.X64Url : info.X86Url;
    }

    public string GetExpectedSha256(UpdateInfo info)
    {
        return Environment.Is64BitProcess ? info.X64Sha256 : info.X86Sha256;
    }

    public Task<string> DownloadPackageAsync(
        UpdateInfo info,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Descărcarea actualizării este efectuată de updater după închiderea aplicației.");
    }

    public void StartUpdater(UpdateInfo info)
    {
        var updater = Path.Combine(
            AppContext.BaseDirectory,
            "CrisGameRoomUpdater.exe");

        if (!File.Exists(updater))
            throw new FileNotFoundException(
                "Actualizatorul Jocuri și Discuții nu este instalat.",
                updater);

        var url = GetPackageUrl(info);
        var sha256 = GetExpectedSha256(info);

        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException(
                "Pachetul pentru această arhitectură nu este disponibil.");

        if (string.IsNullOrWhiteSpace(sha256))
            throw new InvalidOperationException(
                "Manifestul nu conține SHA-256 pentru actualizare.");

        Process.Start(new ProcessStartInfo
        {
            FileName = updater,
            UseShellExecute = true,
            Arguments =
                $"--pid {Environment.ProcessId} " +
                $"--target \"{Environment.ProcessPath}\" " +
                $"--url \"{url}\" " +
                $"--sha256 \"{sha256}\" " +
                $"--version \"{info.Version}\""
        });
    }

    private static async Task<string> ComputeSha256Async(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
