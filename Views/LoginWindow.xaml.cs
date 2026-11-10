using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using CrisGameRoom.Services.Security;
using CrisGameRoom.Services.Updates;
using CrisGameRoom.Localization;
using System.Windows.Controls;
using System.Windows.Automation;
namespace CrisGameRoom.Views;
public partial class LoginWindow : Window
{
    private Task<bool>? _updateGateTask;
    private bool _isAuthenticating;
    private bool _preferencesLoaded;
    private sealed class LoginPreferences { public bool AutoLogin { get; set; } = true; }
    private static string PreferencesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrisGameRoom", "login-preferences.json");
    public LoginWindow()
    {
        InitializeComponent();
        LanguageComboBox.ItemsSource = LocalizationManager.AvailableLanguages
            .Select(x => new LanguageChoice(x.Code, x.Name)).ToArray();
        LanguageComboBox.SelectedValue = LocalizationManager.CurrentLanguage;
        ApplyLanguage();
        LoadPreferences();
    }

    private sealed record LanguageChoice(string Code, string Name);

    private void ApplyLanguage()
    {
        Title = LocalizationManager.Get("nativeTitle");
        LanguageLabel.Text = LocalizationManager.Get("nativeLanguage");
        IdentifierLabel.Text = LocalizationManager.Get("nativeName");
        PasswordLabel.Text = LocalizationManager.Get("nativePassword");
        IdentifierTextBox.SetCurrentValue(AutomationProperties.NameProperty, LocalizationManager.Get("nativeName"));
        PasswordBox.SetCurrentValue(AutomationProperties.NameProperty, LocalizationManager.Get("nativePassword"));
        ConnectButton.Content = LocalizationManager.Get("nativeConnect");
        ForgotPasswordButton.Content = LocalizationManager.Get("nativeForgotPassword");
        CreateAccountButton.Content = LocalizationManager.Get("nativeCreateAccount");
        AboutButton.Content = LocalizationManager.Get("nativeAbout");
        StatusButton.Content = LocalizationManager.Get("nativeStatus");
        ExitButton.Content = LocalizationManager.Get("nativeExit");
        foreach (var button in new[] { ConnectButton, ForgotPasswordButton, CreateAccountButton, AboutButton, StatusButton, ExitButton })
            System.Windows.Automation.AutomationProperties.SetName(button, button.Content?.ToString() ?? "");
        RememberCredentialsCheckBox.Content = LocalizationManager.Get("nativeRememberCredentials");
        AutoLoginCheckBox.Content = LocalizationManager.Get("nativeAutoLogin");
        System.Windows.Automation.AutomationProperties.SetName(RememberCredentialsCheckBox, LocalizationManager.Get("nativeRememberCredentials"));
        System.Windows.Automation.AutomationProperties.SetName(AutoLoginCheckBox, LocalizationManager.Get("nativeAutoLogin"));
        System.Windows.Automation.AutomationProperties.SetName(LanguageComboBox, LocalizationManager.Get("nativeLanguage"));
        IdentifierTextBox.ToolTip = LocalizationManager.Get("nativeName");
        PasswordBox.ToolTip = LocalizationManager.Get("nativePassword");
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedValue is string code && code != LocalizationManager.CurrentLanguage)
        {
            LocalizationManager.SetLanguage(code);
            ApplyLanguage();
        }
    }
    private void LoadPreferences()
    {
        try { if (File.Exists(PreferencesPath)) { var settings = JsonSerializer.Deserialize<LoginPreferences>(File.ReadAllText(PreferencesPath)); AutoLoginCheckBox.IsChecked = settings?.AutoLogin ?? true; } }
        catch { AutoLoginCheckBox.IsChecked = true; }
        _preferencesLoaded = true;
    }
    private void SavePreferences()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!); File.WriteAllText(PreferencesPath, JsonSerializer.Serialize(new LoginPreferences { AutoLogin = AutoLoginCheckBox.IsChecked == true })); }
        catch { }
    }
    private void AutoLoginCheckBox_Changed(object sender, RoutedEventArgs e) { if (_preferencesLoaded) SavePreferences(); }
    private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (!await EnsureUpdateGateAsync())
        {
            ConnectButton.IsEnabled = false;
            return;
        }

        if (CredentialStore.TryLoad(out string savedIdentifier, out string savedPassword))
        {
            IdentifierTextBox.Text = savedIdentifier;
            PasswordBox.Password = savedPassword;
            RememberCredentialsCheckBox.IsChecked = true;
            if (AutoLoginCheckBox.IsChecked == true) await AuthenticateAsync(savedIdentifier, savedPassword);
            else IdentifierTextBox.Focus();
        }
        else IdentifierTextBox.Focus();
    }
    private Task<bool> EnsureUpdateGateAsync() => _updateGateTask ??= UpdateGate.EnsureCurrentAsync(this);
    private async Task AuthenticateAsync(string identifier, string password)
    {
        if (_isAuthenticating) return;
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password)) { IdentifierTextBox.Focus(); return; }
        _isAuthenticating = true;
        ConnectButton.IsEnabled = false;
        try
        {
            if (!await EnsureUpdateGateAsync())
            {
                MessageBox.Show("Nu s-a putut verifica versiunea aplicației. Conectarea a fost oprită.", "Jocuri și Discuții", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var result = await App.Api.LoginAsync(identifier, password);
            if (result is null || string.IsNullOrWhiteSpace(result.AccessToken)) throw new InvalidOperationException("Autentificarea nu a returnat un jeton valid.");
            await App.Sessions.SaveAsync(result);
            if (RememberCredentialsCheckBox.IsChecked == true) CredentialStore.Save(identifier, password);
            else CredentialStore.Delete();
            DialogResult = true;
            Close();
        }
        catch
        {
            MessageBox.Show("Nu am putut să te conectez. Verifică numele de utilizator, parola și conexiunea la internet.", "Autentificare nereușită", MessageBoxButton.OK, MessageBoxImage.Error);
            PasswordBox.Focus();
        }
        finally { ConnectButton.IsEnabled = true; _isAuthenticating = false; }
    }
    private async void ConnectButton_Click(object sender, RoutedEventArgs e) => await AuthenticateAsync(IdentifierTextBox.Text.Trim(), PasswordBox.Password);
    private void ForgotPasswordButton_Click(object sender, RoutedEventArgs e) => OpenWebsite("https://criswebhost.ro/reset-password", "Recuperarea parolei nu este disponibilă momentan.");
    private void CreateAccountButton_Click(object sender, RoutedEventArgs e) => OpenWebsite("https://criswebhost.ro/", "Pagina de creare a contului nu este disponibilă momentan.");
    private static void OpenWebsite(string url, string errorMessage)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch { MessageBox.Show(errorMessage, "Jocuri și Discuții", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("CRIS GameRoom — aplicație pentru jocuri și discuții, construită cu accent pe accesibilitate și utilizarea cu cititoare de ecran.\n\nSite oficial: https://criswebhost.ro", "Despre", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void StatusButton_Click(object sender, RoutedEventArgs e) { var window = new ServiceStatusWindow { Owner = this }; window.ShowDialog(); }
    private void ExitButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
