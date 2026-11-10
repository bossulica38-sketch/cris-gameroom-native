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
    private static string ManifestUrl => System.Environment.Is64BitProcess
        ? "https://criswebhost.ro/downloads/gameroom/stable/auto-update-x64.json"
        : "https://criswebhost.ro/downloads/gameroom/stable/auto-update-x86.json";

    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public Version CurrentVersion =>
        typeof(GameRoomUpdateService).Assembly.GetName().Version
        ?? new Version(0, 0, 0, 0);

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
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            },
            cancellationToken);

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
        var url = GetPackageUrl(info);
        var sha256 = GetExpectedSha256(info);

        if (!System.Uri.TryCreate(url, System.UriKind.Absolute, out var packageUri) ||
            packageUri.Scheme != System.Uri.UriSchemeHttps ||
            !string.Equals(packageUri.Host, "criswebhost.ro",
                System.StringComparison.OrdinalIgnoreCase) ||
            !packageUri.AbsolutePath.StartsWith(
                "/downloads/gameroom/stable/",
                System.StringComparison.OrdinalIgnoreCase) ||
            !packageUri.AbsolutePath.EndsWith(
                ".zip",
                System.StringComparison.OrdinalIgnoreCase))
        {
            throw new System.InvalidOperationException(
                "URL-ul pachetului de actualizare nu este oficial sau nu indică o arhivă ZIP.");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(
            sha256 ?? "",
            "^[0-9a-fA-F]{64}$"))
        {
            throw new System.InvalidOperationException(
                "Manifestul nu conține un SHA-256 valid pentru pachet.");
        }

        var updater = EnsureUpdaterInstalled(info);

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = updater,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(updater)!,
            Arguments =
                $"--pid {Environment.ProcessId} " +
                $"--target \"{Environment.ProcessPath}\" " +
                $"--url \"{url}\" " +
                $"--sha256 \"{sha256}\" " +
                $"--version \"{info.Version}\""
        });

        if (process is null)
            throw new System.InvalidOperationException(
                "Windows nu a putut porni actualizatorul.");
    }

    private string EnsureUpdaterInstalled(UpdateInfo info)
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CrisGameRoom",
            "Updater");

        Directory.CreateDirectory(directory);

        // Fiecare lansare primește un executabil separat. Astfel, o instanță
        // rămasă blocată de Windows nu împiedică descărcarea sau pornirea următoarei.
        string updater = Path.Combine(
            directory,
            $"CrisGameRoomUpdater-{Guid.NewGuid():N}.exe");
        bool is64 = Environment.Is64BitProcess;
        string url = is64 ? info.UpdaterUrlX64 : info.UpdaterUrlX86;
        string expectedHash = is64 ? info.UpdaterSha256X64 : info.UpdaterSha256X86;

        if (!System.Uri.TryCreate(url, System.UriKind.Absolute, out var updaterUri) ||
            updaterUri.Scheme != System.Uri.UriSchemeHttps ||
            !string.Equals(updaterUri.Host, "criswebhost.ro", System.StringComparison.OrdinalIgnoreCase) ||
            !updaterUri.AbsolutePath.StartsWith("/downloads/gameroom/stable/", System.StringComparison.OrdinalIgnoreCase) ||
            !updaterUri.AbsolutePath.EndsWith(".exe", System.StringComparison.OrdinalIgnoreCase) ||
            !System.Text.RegularExpressions.Regex.IsMatch(expectedHash ?? "", "^[0-9a-fA-F]{64}$"))
        {
            throw new System.InvalidOperationException(
                "Manifestul nu conține un URL oficial și un SHA-256 valid pentru actualizator.");
        }

        string temporary = updater + ".download";

        try
        {
            using var http = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };

            using var response = http.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();

            response.EnsureSuccessStatusCode();

            using (var input = response.Content.ReadAsStream())
            {
                using (var output = new FileStream(
                    temporary,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    1024 * 1024,
                    FileOptions.SequentialScan | FileOptions.WriteThrough))
                {
                    input.CopyTo(output);
                    output.Flush(true);
                }
            }

            // Fluxul de scriere este închis înainte de redeschiderea fișierului
            // pentru SHA-256; FileShare.None interzice citirea cât timp e deschis.
            string downloadedHash = ComputeSha256Async(
                temporary,
                CancellationToken.None).GetAwaiter().GetResult();

            if (!string.Equals(
                downloadedHash,
                expectedHash,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new System.Security.Cryptography.CryptographicException(
                    "SHA-256 al actualizatorului descărcat nu corespunde.");
            }

            File.Move(temporary, updater, true);
            return updater;
        }
        finally
        {
            try
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
            catch
            {
            }
        }
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
