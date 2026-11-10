using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace CrisGameRoom.Localization;

public static class LocalizationManager
{
    private static readonly Dictionary<string, Dictionary<string, string>> Languages = LoadLanguages();

    private static readonly Dictionary<string, string> NativeAliases = new(StringComparer.Ordinal)
    {
        ["title"] = "nativeTitle",
        ["name"] = "nativeName",
        ["password"] = "nativePassword",
        ["showPassword"] = "nativeShowPassword",
        ["rememberPassword"] = "nativeRememberCredentials",
        ["rememberCredentials"] = "nativeRememberCredentials",
        ["startWithWindows"] = "nativeStartWithWindows",
        ["language"] = "nativeLanguage",
        ["connect"] = "nativeConnect",
        ["forgotPassword"] = "nativeForgotPassword",
        ["createAccount"] = "nativeCreateAccount",
        ["about"] = "nativeAbout",
        ["status"] = "nativeStatus",
        ["exit"] = "nativeExit",
        ["connecting"] = "nativeConnecting",
        ["available"] = "nativeAvailable",
        ["unavailable"] = "nativeUnavailable",
        ["checkingVersion"] = "nativeCheckingVersion",
        ["version"] = "nativeVersion",
        ["loginError"] = "nativeLoginError",
        ["updateError"] = "nativeUpdateError"
    };

    private static string _currentLanguage = LoadSavedLanguage();

    public static string CurrentLanguage => _currentLanguage;

    public static IReadOnlyList<(string Code, string Name)> AvailableLanguages => new (string, string)[]
    {
        ("ro", "Română"), ("en", "English"), ("pt", "Português"), ("tr", "Türkçe"),
        ("it", "Italiano"), ("es", "Español"), ("de", "Deutsch"), ("fr", "Français"),
        ("nl", "Nederlands"), ("pl", "Polski")
    };

    public static event EventHandler? LanguageChanged;

    private static Dictionary<string, Dictionary<string, string>> LoadLanguages()
    {
        const string resourceName = "CrisGameRoom.Localization.catalog.json";
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Catalogul comun de localizare nu este inclus în aplicație: {resourceName}");

        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement languageRoot = document.RootElement.GetProperty("languages");
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (JsonProperty language in languageRoot.EnumerateObject())
        {
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (JsonProperty entry in language.Value.EnumerateObject())
            {
                if (entry.Value.ValueKind == JsonValueKind.String)
                {
                    string? value = entry.Value.GetString();
                    if (value is not null)
                        entries[entry.Name] = value;
                }
            }

            result[language.Name] = entries;
        }

        string[] expected = ["ro", "en", "pt", "tr", "it", "es", "de", "fr", "nl", "pl"];
        if (result.Count != expected.Length)
            throw new InvalidDataException("Catalogul comun trebuie să conțină exact 10 limbi.");

        foreach (string code in expected)
        {
            if (!result.ContainsKey(code))
                throw new InvalidDataException($"Lipsește limba obligatorie: {code}");
        }

        return result;
    }

    private static string GetLanguageFilePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CrisGameRoom",
            "language.txt");
    }

    private static string LoadSavedLanguage()
    {
        try
        {
            string path = GetLanguageFilePath();
            if (File.Exists(path))
            {
                string saved = File.ReadAllText(path).Trim().ToLowerInvariant();
                if (Languages.ContainsKey(saved))
                    return saved;
            }
        }
        catch
        {
        }

        return "ro";
    }

    public static void SetLanguage(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return;

        string normalized = code.Trim().ToLowerInvariant();
        if (!Languages.ContainsKey(normalized))
            return;

        _currentLanguage = normalized;
        ApplyCulture(normalized);
        LanguageChanged?.Invoke(null, EventArgs.Empty);

        try
        {
            string path = GetLanguageFilePath();
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, normalized);
        }
        catch
        {
        }
    }

    public static void ApplyCurrentCulture() => ApplyCulture(_currentLanguage);

    private static void ApplyCulture(string code)
    {
        string cultureName = code switch
        {
            "ro" => "ro-RO", "en" => "en-US", "pt" => "pt-PT", "tr" => "tr-TR",
            "it" => "it-IT", "es" => "es-ES", "de" => "de-DE", "fr" => "fr-FR",
            "nl" => "nl-NL", "pl" => "pl-PL", _ => "ro-RO"
        };
        CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    public static string Get(string key)
    {
        string resolvedKey = NativeAliases.TryGetValue(key, out string? nativeKey)
            ? nativeKey
            : key;

        if (Languages.TryGetValue(_currentLanguage, out var language) &&
            language.TryGetValue(resolvedKey, out string? value))
            return value;

        return Languages.TryGetValue("ro", out var romanian) &&
               romanian.TryGetValue(resolvedKey, out string? fallback)
            ? fallback
            : key;
    }
}
