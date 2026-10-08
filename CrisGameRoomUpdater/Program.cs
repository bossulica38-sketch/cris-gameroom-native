using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

internal static class Program
{
    private static int Main(string[] args)
    {
        string log = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CrisGameRoom",
            "update.log");

        Directory.CreateDirectory(Path.GetDirectoryName(log)!);

        try
        {
            Log(log, "UPDATER_START");

            var pidText = Get(args, "--pid");
            var target = Get(args, "--target");
            var url = Get(args, "--url");
            var expectedSha256 = Get(args, "--sha256");
            var expectedVersion = Get(args, "--version");

            Log(log, $"PID={pidText}");
            Log(log, $"TARGET={target}");
            Log(log, $"URL={url}");
            Log(log, $"EXPECTED_SHA256={expectedSha256}");
            Log(log, $"EXPECTED_VERSION={expectedVersion}");

            if (!int.TryParse(pidText, out var processId) ||
                string.IsNullOrWhiteSpace(target) ||
                string.IsNullOrWhiteSpace(url) ||
                string.IsNullOrWhiteSpace(expectedSha256))
                return Fail(log, 2, "ARGUMENTE_INVALIDE");

            if (!File.Exists(target))
                return Fail(log, 4, "APLICATIE_TINTA_LIPSA");

            var directory = Path.GetDirectoryName(target);
            if (string.IsNullOrWhiteSpace(directory))
                return Fail(log, 5, "DIRECTOR_TINTA_INVALID");

            Log(log, "WAITING_FOR_APPLICATION_EXIT");

            if (!WaitForProcessExit(processId, log))
                return Fail(log, 6, "PROCESUL_APLICATIEI_NU_S_A_INCHIS");

            Log(log, "APPLICATION_PROCESS_EXITED");

            var backup = target + ".previous";
            var staged = target + ".staged";
            var package = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CrisGameRoom",
                "Updates",
                $"JocuriSiDiscutii-{expectedVersion}-{(Environment.Is64BitOperatingSystem ? "x64" : "x86")}.download");

            Directory.CreateDirectory(Path.GetDirectoryName(package)!);

            TryDelete(staged);
            TryDelete(package);

            Log(log, "DOWNLOAD_AFTER_APPLICATION_EXIT");

            using (var http = new System.Net.Http.HttpClient())
            {
                http.Timeout = TimeSpan.FromMinutes(10);

                using var response = http.GetAsync(
                    url,
                    System.Net.Http.HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();

                response.EnsureSuccessStatusCode();

                using var input = response.Content.ReadAsStream();
                using var output = new FileStream(
                    package,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    1024 * 1024,
                    FileOptions.SequentialScan | FileOptions.WriteThrough);

                input.CopyTo(output);
                output.Flush(true);
            }

            Log(log, "DOWNLOAD_COMPLETE");

            string actualSha256;
            using (var stream = File.OpenRead(package))
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                actualSha256 = Convert.ToHexString(
                    sha.ComputeHash(stream)).ToLowerInvariant();
            }

            if (!string.Equals(
                    actualSha256,
                    expectedSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                TryDelete(package);
                return Fail(log, 7, "SHA256_INVALID");
            }

            Log(log, "SHA256_VALID");

            Log(log, "COPY_PACKAGE_TO_STAGED");

            if (!RetryFileOperation(
                    () => File.Copy(package, staged, true),
                    60,
                    500,
                    log,
                    "COPY_STAGED"))
                return Fail(log, 8, "COPIERE_STAGED_ESUATA");

            Log(log, "REPLACE_APPLICATION");

            bool replaced = false;

            for (int attempt = 1; attempt <= 60; attempt++)
            {
                try
                {
                    if (File.Exists(backup))
                        File.Delete(backup);

                    File.Move(target, backup);
                    Log(log, $"BACKUP_CREATED_ATTEMPT={attempt}");

                    File.Move(staged, target);
                    Log(log, $"APPLICATION_REPLACED_ATTEMPT={attempt}");

                    replaced = true;
                    break;
                }
                catch (Exception ex)
                {
                    Log(log,
                        $"REPLACE_RETRY={attempt} " +
                        $"TYPE={ex.GetType().Name} " +
                        $"MESSAGE={ex.Message}");

                    if (File.Exists(backup) && !File.Exists(target))
                    {
                        try
                        {
                            File.Move(backup, target, true);
                        }
                        catch (Exception rollbackEx)
                        {
                            Log(log,
                                $"ROLLBACK_RETRY_FAILED={rollbackEx.Message}");
                        }
                    }

                    Thread.Sleep(1000);
                }
            }

            if (!replaced)
                return Fail(log, 9, "EXE_BLOCAT_SAU_INLOCUIRE_ESUATA");

            TryDelete(package);

            Log(log, "UPDATE_REPLACED_SUCCESSFULLY");
            Log(log, "STARTING_UPDATED_APPLICATION");

            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true,
                    WorkingDirectory = directory
                });

                if (process is null)
                    throw new InvalidOperationException(
                        "Windows nu a putut porni aplicația actualizată.");

                Log(log, $"UPDATED_PROCESS_STARTED_PID={process.Id}");
                Log(log, "UPDATE_SUCCESS");

                return 0;
            }
            catch (Exception ex)
            {
                Log(log,
                    $"UPDATED_START_FAILED={ex.GetType().Name}: {ex.Message}");

                if (File.Exists(backup))
                {
                    try
                    {
                        File.Delete(target);
                        File.Move(backup, target, true);
                        Log(log, "ROLLBACK_SUCCESS");
                    }
                    catch (Exception rollbackEx)
                    {
                        Log(log,
                            $"ROLLBACK_FAILED={rollbackEx.GetType().Name}: {rollbackEx.Message}");
                    }
                }

                return Fail(log, 10, "PORNIRE_UPDATE_ESUATA");
            }
        }
        catch (Exception ex)
        {
            return Fail(log, 99,
                $"EXCEPTIE_NEASTEPTATA={ex.GetType().Name}: {ex.Message}");
        }
    }

    private static bool WaitForProcessExit(int processId, string log)
    {
        try
        {
            using var process = Process.GetProcessById(processId);

            for (int second = 1; second <= 120; second++)
            {
                if (process.HasExited)
                {
                    Log(log, $"ORIGINAL_PROCESS_EXITED_AFTER={second}s");
                    return true;
                }

                if (second % 5 == 0)
                    Log(log, $"PROCESS_STILL_RUNNING={second}s");

                Thread.Sleep(1000);
            }

            return false;
        }
        catch (ArgumentException)
        {
            Log(log, "ORIGINAL_PROCESS_ALREADY_EXITED");
            return true;
        }
    }

    private static bool RetryFileOperation(
        Action action,
        int attempts,
        int delay,
        string log,
        string operation)
    {
        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                action();
                Log(log, $"{operation}_SUCCESS_ATTEMPT={attempt}");
                return true;
            }
            catch (Exception ex)
            {
                Log(log,
                    $"{operation}_RETRY={attempt} " +
                    $"TYPE={ex.GetType().Name} " +
                    $"MESSAGE={ex.Message}");

                Thread.Sleep(delay);
            }
        }

        return false;
    }

    private static string Get(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(
                    args[i],
                    name,
                    StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return "";
    }

    private static int Fail(string log, int code, string message)
    {
        Log(log, $"UPDATE_FAILED_CODE={code}");
        Log(log, message);
        return code;
    }

    private static void Log(string path, string message)
    {
        try
        {
            File.AppendAllText(
                path,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }
}
