using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using CrisGameRoom.Services.Authentication;
using CrisGameRoom.Localization;

namespace CrisGameRoom.Views;

public partial class MainWindow : Window
{
    private bool _initialized;
    private bool _loggingOut;

    public MainWindow()
    {
        InitializeComponent();

        PreviewKeyDown += MainWindow_PreviewKeyDown;
        LocalizationManager.LanguageChanged += LocalizationManager_LanguageChanged;
        ApplyLanguage();
    }

    private void LocalizationManager_LanguageChanged(object? sender, EventArgs e) => ApplyLanguage();

    protected override void OnClosed(EventArgs e)
    {
        LocalizationManager.LanguageChanged -= LocalizationManager_LanguageChanged;
        base.OnClosed(e);
    }

    private void ApplyLanguage()
    {
        Title = LocalizationManager.Get("nativeGameRoom");
        TitleText.Text = LocalizationManager.Get("nativeGameRoom");
        CreateTableButton.Content = LocalizationManager.Get("nativeCreateTable");
        JoinTableButton.Content = LocalizationManager.Get("nativeJoinTable");
        RestoreTableButton.Content = LocalizationManager.Get("nativeRestoreTable");
        MyTablesButton.Content = LocalizationManager.Get("nativeMyTables");
        OptionsButton.Content = LocalizationManager.Get("nativeOptions");
        FeaturesButton.Content = LocalizationManager.Get("nativeFeatures");
        HelpButton.Content = LocalizationManager.Get("nativeHelp");
        StatusButton.Content = LocalizationManager.Get("nativeStatusServices");
        ExitGameRoomButton.Content = LocalizationManager.Get("nativeExitGameRoom");
        foreach (var button in new[] { CreateTableButton, JoinTableButton, RestoreTableButton, MyTablesButton, OptionsButton, FeaturesButton, HelpButton, StatusButton, ExitGameRoomButton })
            System.Windows.Automation.AutomationProperties.SetName(button, button.Content?.ToString() ?? "");
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        try
        {
            var session = await App.Sessions.LoadAsync();

            if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
            {
                ConnectionText.Text = LocalizationManager.Get("nativeSessionUnavailable");
                MessageBox.Show(
                    LocalizationManager.Get("nativeSessionUnavailable"),
                    "Jocuri în Camera",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                Close();
                return;
            }

            App.Api.SetAccessToken(session.AccessToken);

            ConnectionText.Text = "Se verifică autentificarea...";

            var currentUser = await App.Api.GetCurrentUserAsync();

            if (currentUser == null)
            {
                ConnectionText.Text = LocalizationManager.Get("nativeSessionExpired");

                MessageBox.Show(
                    LocalizationManager.Get("nativeSessionExpired"),
                    "Jocuri în Camera",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                await App.Realtime.DisconnectAsync();
                Close();
                return;
            }

            UserText.Text = LocalizationManager.Get("nativeConnectedGameRoom");
            ConnectionText.Text = "Conectat";

            await App.Realtime.ConnectAsync(session.AccessToken);

            ConnectionText.Text = "Conectat";

            VersionText.Text =
                $"{LocalizationManager.Get("nativeGameRoom")} • {typeof(MainWindow).Assembly.GetName().Version}";
        }
        catch (Exception ex)
        {
            ConnectionText.Text = LocalizationManager.Get("nativeConnectionUnavailable");

            MessageBox.Show(
                "Nu s-a putut inițializa Camera de joc.\n\n" +
                ex.Message,
                "Jocuri în Camera",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            try
            {
                await App.Realtime.DisconnectAsync();
            }
            catch
            {
            }

            Close();
        }
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F4 &&
            System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Alt)
        {
            e.Handled = true;
            Close();
            return;
        }

        if (e.Key == System.Windows.Input.Key.Escape)
        {
            e.Handled = true;
        }
    }

    private void CreateTableButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Crearea unei table noi va fi deschisă aici.\n\n" +
            "Tabla va fi PUBLICĂ în mod implicit. " +
            "Ctrl+H va comuta între PUBLICĂ și PRIVATĂ.",
            "Creează o tablă nouă",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void JoinTableButton_Click(object sender, RoutedEventArgs e)
    {
        var chooser = new JoinTableWindow { Owner = this };

        if (chooser.ShowDialog() != true || chooser.SelectedRoom is null)
            return;

        string code = chooser.SelectedRoom.Code;

        if (string.IsNullOrWhiteSpace(code))
        {
            MessageBox.Show(
                LocalizationManager.Get("nativeJoinRoomError"),
                LocalizationManager.Get("nativeJoinWindowTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        bool joinedThroughApi = false;

        try
        {
            ConnectionText.Text = LocalizationManager.Get("nativeJoinRoomProgress");

            await App.Api.JoinRoomAsync(code);
            joinedThroughApi = true;

            if (!App.Realtime.IsConnected)
                throw new InvalidOperationException("RealtimeDisconnected");

            await App.Realtime.JoinRoomAsync(code);

            var currentRoom = await App.Api.GetRoomAsync(code);
            var tableWindow = new RoomTableWindow(currentRoom) { Owner = this };

            Hide();

            tableWindow.Closed += (_, _) =>
            {
                if (!IsVisible)
                    Show();

                Activate();
                ConnectionText.Text = App.Realtime.IsConnected
                    ? "Conectat"
                    : "Conexiune în timp real indisponibilă";
            };

            tableWindow.Show();
        }
        catch
        {
            if (joinedThroughApi)
            {
                try { await App.Api.LeaveRoomAsync(code); } catch { }
                try { await App.Realtime.LeaveRoomAsync(code); } catch { }
            }

            if (!IsVisible)
                Show();

            Activate();
            ConnectionText.Text = App.Realtime.IsConnected
                ? "Conectat"
                : "Conexiune în timp real indisponibilă";

            MessageBox.Show(
                LocalizationManager.Get("nativeJoinRoomError"),
                LocalizationManager.Get("nativeJoinWindowTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RestoreTableButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Aici se va deschide restaurarea unei table.",
            "Restabilește o tablă",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void MyTablesButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Aici vor fi afișate tablele tale active și salvate.",
            "Tablele mele",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OptionsButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Aici vor fi disponibile opțiunile aplicației.",
            "Opțiuni",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void FeaturesButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Aici vor fi disponibile funcțiile suplimentare.",
            "Funcții și altele",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Aici va fi disponibil ajutorul pentru Camera de joc.",
            "Ajutor",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void StatusButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Camera de joc este conectată la serviciile aplicației.\n\n" +
            "Autentificare: disponibilă\n" +
            "Conexiune timp real: activă",
            "Status și servicii",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void ExitGameRoomButton_Click(object sender, RoutedEventArgs e)
    {
        if (_loggingOut)
        {
            return;
        }

        _loggingOut = true;
        ExitGameRoomButton.IsEnabled = false;
        ConnectionText.Text = "Se închide sesiunea...";

        try
        {
            await App.Realtime.DisconnectAsync();
        }
        catch
        {
        }

        try
        {
            await App.Api.LogoutAsync();
        }
        catch
        {
        }

        App.Api.SetAccessToken(null);

        Close();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_loggingOut)
        {
            return;
        }
    }
}
