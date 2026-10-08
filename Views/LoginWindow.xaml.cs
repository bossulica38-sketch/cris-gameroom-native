using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CrisGameRoom.Localization;
using CrisGameRoom.Services.Updates;

namespace CrisGameRoom.Views;

public partial class LoginWindow : Window
{
    private bool _loadingLanguage;

    public LoginWindow()
    {
        InitializeComponent();
    }

    private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        IdentifierTextBox.Focus();
        await CheckServiceAndVersionAsync();
    }

    private async Task CheckServiceAndVersionAsync()
    {
        ConnectionStatusText.Text = LocalizationManager.Get("connecting");
        VersionStatusText.Text = LocalizationManager.Get("checkingVersion");

        try
        {
            var currentUser = await App.Api.GetCurrentUserAsync();
            ConnectionStatusText.Text = LocalizationManager.Get("available");
        }
        catch
        {
            ConnectionStatusText.Text = LocalizationManager.Get("available");
        }

        try
        {
            await UpdateGate.EnsureCurrentAsync(this);
            VersionStatusText.Text =
                $"{LocalizationManager.Get("version")} {GetType().Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}";
        }
        catch
        {
            VersionStatusText.Text = LocalizationManager.Get("updateError");
        }
    }

    private void ShowPasswordCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        VisiblePasswordBox.Text = PasswordBox.Password;
        PasswordBox.Visibility = Visibility.Collapsed;
        VisiblePasswordBox.Visibility = Visibility.Visible;
        VisiblePasswordBox.Focus();
        VisiblePasswordBox.CaretIndex = VisiblePasswordBox.Text.Length;
    }

    private void ShowPasswordCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        PasswordBox.Password = VisiblePasswordBox.Text;
        VisiblePasswordBox.Visibility = Visibility.Collapsed;
        PasswordBox.Visibility = Visibility.Visible;
        PasswordBox.Focus();
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingLanguage || LanguageComboBox.SelectedItem is not ComboBoxItem item)
            return;

        var code = item.Tag?.ToString();
        if (string.IsNullOrWhiteSpace(code))
            return;

        _loadingLanguage = true;
        LocalizationManager.SetLanguage(code);
        ApplyLanguage();
        _loadingLanguage = false;
    }

    private void ApplyLanguage()
    {
        TitleText.Text = LocalizationManager.Get("title");
        Title = LocalizationManager.Get("title");

        NameLabel.Text = LocalizationManager.Get("name");
        PasswordLabel.Text = LocalizationManager.Get("password");

        ShowPasswordCheckBox.Content = LocalizationManager.Get("showPassword");
        RememberPasswordCheckBox.Content = LocalizationManager.Get("rememberPassword");
        StartWithWindowsCheckBox.Content = LocalizationManager.Get("startWithWindows");

        LanguageLabel.Text = LocalizationManager.Get("language");
        ConnectButton.Content = LocalizationManager.Get("connect");
        ForgotPasswordButton.Content = LocalizationManager.Get("forgotPassword");
        CreateAccountButton.Content = LocalizationManager.Get("createAccount");
        AboutButton.Content = LocalizationManager.Get("about");
        StatusButton.Content = LocalizationManager.Get("status");
        ExitButton.Content = LocalizationManager.Get("exit");
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        var identifier = IdentifierTextBox.Text.Trim();
        var password = PasswordBox.Password;

        if (ShowPasswordCheckBox.IsChecked == true)
            password = VisiblePasswordBox.Text;

        if (string.IsNullOrWhiteSpace(identifier) ||
            string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show(
                LocalizationManager.Get("loginError"),
                LocalizationManager.Get("title"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        ConnectButton.IsEnabled = false;
        ConnectionStatusText.Text = LocalizationManager.Get("connecting");

        try
        {
            var result = await App.Api.LoginAsync(identifier, password);

            if (result is null || string.IsNullOrWhiteSpace(result.AccessToken))
                throw new InvalidOperationException("Authentication returned no access token.");

            await App.Sessions.SaveAsync(result);

            ConnectionStatusText.Text = LocalizationManager.Get("available");

            DialogResult = true;
            Close();
        }
        catch
        {
            ConnectionStatusText.Text = LocalizationManager.Get("unavailable");

            MessageBox.Show(
                LocalizationManager.Get("loginError"),
                LocalizationManager.Get("title"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            ConnectButton.IsEnabled = true;
        }
    }

    private void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            LocalizationManager.Get("forgotPassword"),
            LocalizationManager.Get("title"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            LocalizationManager.Get("createAccount"),
            LocalizationManager.Get("title"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Cris GameRoom",
            LocalizationManager.Get("about"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void StatusButton_Click(object sender, RoutedEventArgs e)
    {
        _ = CheckServiceAndVersionAsync();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
