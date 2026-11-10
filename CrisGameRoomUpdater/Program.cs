using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;

internal static class Program
{
    private sealed class Change
    {
        public required string Destination { get; init; }
        public required string Backup { get; init; }
        public required bool Existed { get; init; }
    }

    private static void Log(string path, string message)
    {
        try
        {
            File.AppendAllText(
                path,
                $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    private static string Get(string[] args, string key)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return "";
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static bool WaitForProcessExit(int processId, string target, string log)
    {
        string expected = Path.GetFullPath(target);
        string processName = Path.GetFileNameWithoutExtension(expected);
        var matching = new List<Process>();

        // Închidem toate instanțele aceleiași aplicații din aceeași locație,
        // nu doar PID-ul inițial. O instanță secundară poate ține EXE-ul blocat.
        for (int attempt = 1; attempt <= 15; attempt++)
        {
            matching.Clear();
            foreach (var candidate in Process.GetProcessesByName(processName))
            {
                try
                {
                    string? path = candidate.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(path) &&
                        string.Equals(Path.GetFullPath(path), expected,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        matching.Add(candidate);
                    }
                    else candidate.Dispose();
                }
                catch (Exception ex)
                {
                    Log(log, $"PROCESS_ENUM_WARNING={candidate.Id} {ex.GetType().Name}: {ex.Message}");
                    candidate.Dispose();
                }
            }

            if (matching.Count == 0)
            {
                Log(log, $"ALL_TARGET_INSTANCES_EXITED_ATTEMPT={attempt}");
                return true;
            }

            if (attempt == 1)
                Log(log, $"MATCHING_TARGET_INSTANCES={matching.Count}; REQUESTING_CLOSE");

            foreach (var process in matching)
            {
                try
                {
                    if (!process.HasExited) process.CloseMainWindow();
                }
                catch (Exception ex)
                {
                    Log(log, $"CLOSE_REQUEST_WARNING={process.Id} {ex.Message}");
                }
            }

            Thread.Sleep(1000);
        }

        // Procesele confirmate după calea completă sunt singurele terminate.
        matching.Clear();
        foreach (var candidate in Process.GetProcessesByName(processName))
        {
            try
            {
                string? path = candidate.MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(path) &&
                    string.Equals(Path.GetFullPath(path), expected,
                        StringComparison.OrdinalIgnoreCase))
                    matching.Add(candidate);
                else candidate.Dispose();
            }
            catch { candidate.Dispose(); }
        }

        foreach (var process in matching)
        {
            try
            {
                if (!process.HasExited)
                {
                    Log(log, $"FORCE_TERMINATING_TARGET_PID={process.Id}");
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex)
            {
                Log(log, $"FORCE_TERMINATE_WARNING={process.Id} {ex.GetType().Name}: {ex.Message}");
            }
        }

        foreach (var process in matching)
        {
            try { process.WaitForExit(10000); }
            catch { }
            process.Dispose();
        }

        bool parentExited;
        try { using var parent = Process.GetProcessById(processId); parentExited = parent.HasExited; }
        catch (ArgumentException) { parentExited = true; }
        catch { parentExited = false; }

        bool anyRemaining = Process.GetProcessesByName(processName).Any(p =>
        {
            try
            {
                bool same = string.Equals(Path.GetFullPath(p.MainModule?.FileName ?? ""), expected,
                    StringComparison.OrdinalIgnoreCase);
                p.Dispose();
                return same;
            }
            catch { p.Dispose(); return false; }
        });

        Log(log, $"PARENT_EXITED={parentExited}; TARGET_INSTANCES_REMAIN={anyRemaining}");
        return parentExited && !anyRemaining;
    }

    private static bool IsInsideDirectory(string path, string directory)
    {
        string normalizedDirectory = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        string normalizedPath = Path.GetFullPath(path);

        return normalizedPath.StartsWith(
            normalizedDirectory,
            StringComparison.OrdinalIgnoreCase);
    }

    private static void CleanupDirectory(string path, string log)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch (Exception ex)
        {
            Log(log, $"CLEANUP_WARNING={path} {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static int Main(string[] args)
    {
        string localRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CrisGameRoom");

        string updateRoot = Path.Combine(localRoot, "Updates");
        string log = Path.Combine(localRoot, "update.log");

        string archive = "";
        string extraction = "";
        string rollbackRoot = "";
        string target = "";
        var changes = new List<Change>();
        bool rollbackFailed = false;

        Directory.CreateDirectory(updateRoot);
        Log(log, "UPDATER_START");

        try
        {
            string pidText = Get(args, "--pid");
            target = Get(args, "--target");
            string url = Get(args, "--url");
            string expectedSha256 = Get(args, "--sha256");
            string expectedVersion = Get(args, "--version");

            Log(log, $"PID={pidText}");
            Log(log, $"TARGET={target}");
            Log(log, $"URL={url}");
            Log(log, $"EXPECTED_VERSION={expectedVersion}");

            if (!int.TryParse(pidText, out int processId) ||
                string.IsNullOrWhiteSpace(target) ||
                string.IsNullOrWhiteSpace(url) ||
                !Regex.IsMatch(expectedSha256, "^[0-9a-fA-F]{64}$") ||
                !Version.TryParse(expectedVersion, out _))
            {
                throw new InvalidOperationException("ARGUMENTE_SAU_VERSIUNE_INVALIDE");
            }

            target = Path.GetFullPath(target);

            if (!string.Equals(
                    Path.GetFileName(target),
                    "JocuriSiDiscutii.exe",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("EXECUTABIL_ȚINTĂ_NEACCEPTAT");
            }

            if (!File.Exists(target))
                throw new FileNotFoundException("APLICAȚIE_ȚINTĂ_LIPSĂ", target);

            if (!Uri.TryCreate(url, UriKind.Absolute, out var packageUri) ||
                packageUri.Scheme != Uri.UriSchemeHttps ||
                !string.Equals(
                    packageUri.Host,
                    "criswebhost.ro",
                    StringComparison.OrdinalIgnoreCase) ||
                !packageUri.AbsolutePath.StartsWith(
                    "/downloads/gameroom/stable/",
                    StringComparison.OrdinalIgnoreCase) ||
                !packageUri.AbsolutePath.EndsWith(
                    ".zip",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("URL_PACHET_NEACCEPTAT");
            }

            if (!WaitForProcessExit(processId, target, log))
                throw new TimeoutException("PROCESUL_APLICAȚIEI_NU_S_A_ÎNCHIS");

            string installDirectory = Path.GetDirectoryName(target)
                ?? throw new InvalidOperationException("DIRECTOR_ȚINTĂ_INVALID");

            string runId = Guid.NewGuid().ToString("N");
            archive = Path.Combine(updateRoot, $"update-{runId}.zip");
            extraction = Path.Combine(updateRoot, $"extract-{runId}");
            rollbackRoot = Path.Combine(updateRoot, $"rollback-{runId}");

            Directory.CreateDirectory(extraction);
            Directory.CreateDirectory(rollbackRoot);

            Log(log, "DOWNLOAD_START");

            using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
            using (var response = http.GetAsync(
                packageUri,
                HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
            {
                response.EnsureSuccessStatusCode();

                using var input = response.Content.ReadAsStream();
                using var output = new FileStream(
                    archive,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    1024 * 1024,
                    FileOptions.SequentialScan | FileOptions.WriteThrough);

                input.CopyTo(output);
                output.Flush(true);
            }

            Log(log, "DOWNLOAD_COMPLETE");

            string actualHash = Sha256(archive);
            if (!string.Equals(
                actualHash,
                expectedSha256,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("SHA256_INVALID");
            }

            Log(log, "SHA256_VALID");

            ZipFile.ExtractToDirectory(archive, extraction, true);

            string newExecutable = Path.Combine(extraction, "JocuriSiDiscutii.exe");
            if (!File.Exists(newExecutable) || new FileInfo(newExecutable).Length < 100_000)
                throw new InvalidDataException("ARHIVĂ_FĂRĂ_EXECUTABIL_VALID");

            Log(log, "ARCHIVE_EXTRACTED");

            foreach (string sourceFile in Directory.EnumerateFiles(
                extraction, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(extraction, sourceFile);
                string normalizedRelative = relative.Replace('\\', '/');

                if (Path.IsPathRooted(relative) ||
                    normalizedRelative.Split('/').Any(
                        part => part == ".." || part == "."))
                {
                    throw new InvalidDataException("CALE_ARHIVĂ_NEVALIDĂ");
                }

                // Actualizatorul nu își înlocuiește propriul executabil în timp ce rulează.
                if (string.Equals(
                    normalizedRelative,
                    "updater/CrisGameRoomUpdater.exe",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string destination = Path.GetFullPath(
                    Path.Combine(installDirectory, relative));

                if (!string.Equals(
                    Path.GetDirectoryName(destination),
                    installDirectory,
                    StringComparison.OrdinalIgnoreCase) &&
                    !IsInsideDirectory(destination, installDirectory))
                {
                    throw new InvalidDataException("CALE_DESTINAȚIE_NEVALIDĂ");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                bool existed = File.Exists(destination);
                string backup = Path.Combine(rollbackRoot, relative);

                if (existed)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                    File.Copy(destination, backup, true);
                }

                // Înregistrăm schimbarea înainte de înlocuire pentru rollback la eroare.
                changes.Add(new Change
                {
                    Destination = destination,
                    Backup = backup,
                    Existed = existed
                });

                string temporary = destination + "." + runId + ".staged";

                try
                {
                    File.Copy(sourceFile, temporary, true);
                    File.Move(temporary, destination, true);
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

            if (!File.Exists(target))
                throw new IOException("EXECUTABILUL_FINAL_LIPSEȘTE");

            Log(log, "ALL_FILES_REPLACED");

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = target,
                WorkingDirectory = installDirectory,
                UseShellExecute = true
            });

            if (process is null)
                throw new InvalidOperationException("APLICAȚIA_ACTUALIZATĂ_NU_A_PORNIT");

            Log(log, $"UPDATED_PROCESS_STARTED_PID={process.Id}");
            Log(log, "UPDATE_SUCCESS");

            CleanupDirectory(extraction, log);
            CleanupDirectory(rollbackRoot, log);

            try
            {
                if (File.Exists(archive))
                    File.Delete(archive);
            }
            catch (Exception ex)
            {
                Log(log, $"ARCHIVE_CLEANUP_WARNING={ex.Message}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Log(log, $"UPDATE_ERROR={ex.GetType().Name}: {ex.Message}");

            foreach (Change change in changes.AsEnumerable().Reverse())
            {
                try
                {
                    if (change.Existed)
                    {
                        if (!File.Exists(change.Backup))
                            throw new IOException("FIȘIER_ROLLBACK_LIPSĂ: " + change.Backup);

                        Directory.CreateDirectory(
                            Path.GetDirectoryName(change.Destination)!);

                        File.Copy(change.Backup, change.Destination, true);
                    }
                    else if (File.Exists(change.Destination))
                    {
                        File.Delete(change.Destination);
                    }
                }
                catch (Exception rollbackEx)
                {
                    rollbackFailed = true;
                    Log(log,
                        $"ROLLBACK_ERROR={change.Destination} " +
                        $"{rollbackEx.GetType().Name}: {rollbackEx.Message}");
                }
            }

            Log(log, rollbackFailed ? "ROLLBACK_INCOMPLETE" : "ROLLBACK_FINISHED");

            CleanupDirectory(extraction, log);

            if (!rollbackFailed)
                CleanupDirectory(rollbackRoot, log);
            else
                Log(log, $"ROLLBACK_FILES_RETAINED={rollbackRoot}");

            try
            {
                if (!string.IsNullOrWhiteSpace(archive) && File.Exists(archive))
                    File.Delete(archive);
            }
            catch
            {
            }

            return 99;
        }
    }
}
