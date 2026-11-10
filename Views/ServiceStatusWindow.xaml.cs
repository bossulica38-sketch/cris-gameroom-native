using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using CrisGameRoom.Localization;
namespace CrisGameRoom.Views;
public partial class ServiceStatusWindow : Window
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(8) };
    public ServiceStatusWindow()
    {
        InitializeComponent();
        ApplyLanguage();
        LocalizationManager.LanguageChanged += LocalizationManager_LanguageChanged;
    }

    private void LocalizationManager_LanguageChanged(object? sender, EventArgs e) => ApplyLanguage();

    protected override void OnClosed(EventArgs e)
    {
        LocalizationManager.LanguageChanged -= LocalizationManager_LanguageChanged;
        base.OnClosed(e);
    }

    private void ApplyLanguage()
    {
        Title = LocalizationManager.Get("nativeStatus");
        StatusTitleText.Text = LocalizationManager.Get("nativeStatus");
        RefreshButton.Content = LocalizationManager.Get("nativeRoomsRefresh");
    }
    private async void Window_Loaded(object sender, RoutedEventArgs e) => await RefreshAsync();
    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await RefreshAsync();
    private async Task<bool> CheckAsync(string url, bool authResponseIsOnline = false)
    {
        try { using var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead); return response.IsSuccessStatusCode || (authResponseIsOnline && (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)); }
        catch { return false; }
    }
    private async Task RefreshAsync()
    {
        RefreshButton.IsEnabled = false;
        LastCheckedText.Text = LocalizationManager.Get("nativeCheckingVersion");
        try
        {
            var checks = await Task.WhenAll(CheckAsync("https://criswebhost.ro/"), CheckAsync("https://criswebhost.ro/api/auth/me", true), CheckAsync("https://criswebhost.ro/downloads/gameroom/stable/latest.json"));
            PlatformStatusText.Text = LocalizationManager.Get("nativeServicePlatform") + ": " + (checks[0] ? LocalizationManager.Get("nativeServiceOnline") : LocalizationManager.Get("nativeServiceOffline"));
            ApiStatusText.Text = LocalizationManager.Get("nativeServiceApi") + ": " + (checks[1] ? LocalizationManager.Get("nativeServiceOnline") : LocalizationManager.Get("nativeServiceOffline"));
            UpdatesStatusText.Text = LocalizationManager.Get("nativeServiceUpdater") + ": " + (checks[2] ? LocalizationManager.Get("nativeServiceOnline") : LocalizationManager.Get("nativeServiceOffline"));
            LastCheckedText.Text = $"Ultima verificare: {DateTime.Now:dd.MM.yyyy, HH:mm:ss}";
        }
        finally { RefreshButton.IsEnabled = true; }
    }
}
