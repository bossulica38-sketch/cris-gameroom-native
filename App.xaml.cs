using System.Windows;
using CrisGameRoom.Localization;
using CrisGameRoom.Services.Api;
using CrisGameRoom.Services.Authentication;
using CrisGameRoom.Services.Realtime;
using System.Windows.Input;

namespace CrisGameRoom;

public partial class App : Application
{
    public static GameRoomApiClient Api { get; } = new();
    public static SessionStore Sessions { get; } = new();
    public static GameRoomRealtimeClient Realtime { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        EventManager.RegisterClassHandler(
            typeof(Window),
            Keyboard.PreviewKeyDownEvent,
            new KeyEventHandler(GlobalWindow_PreviewKeyDown),
            true);
        LocalizationManager.ApplyCurrentCulture();

        var logDirectory = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CrisGameRoom");

        try
        {
            System.IO.Directory.CreateDirectory(logDirectory);
        }
        catch
        {
        }

        DispatcherUnhandledException += (_, args) =>
        {
            try
            {
                System.IO.Directory.CreateDirectory(logDirectory);
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(logDirectory, "crash.log"),
                    $"[{DateTime.Now:O}] DispatcherUnhandledException{Environment.NewLine}" +
                    $"{args.Exception}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
            }

            MessageBox.Show(
                "Aplicația a întâmpinat o eroare neașteptată. Detaliile au fost salvate în jurnal.",
                "Jocuri și Discuții",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            try
            {
                System.IO.Directory.CreateDirectory(logDirectory);
                var exceptionText = args.ExceptionObject?.ToString() ?? "Excepție necunoscută";
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(logDirectory, "crash.log"),
                    $"[{DateTime.Now:O}] UnhandledException{Environment.NewLine}" +
                    $"{exceptionText}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
            }
        };

        try
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var login = new Views.LoginWindow();
            MainWindow = login;

            var loginResult = login.ShowDialog();

            if (loginResult == true)
            {
                var window = new Views.MainWindow();
                MainWindow = window;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                window.Show();
                window.Activate();
            }
            else
            {
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            try
            {
                System.IO.Directory.CreateDirectory(logDirectory);
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(logDirectory, "crash.log"),
                    $"[{DateTime.Now:O}] StartupException{Environment.NewLine}" +
                    $"{ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
            }

            MessageBox.Show(
                "Aplicația nu a putut porni. Detaliile au fost salvate în jurnal.",
                "Jocuri și Discuții",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);
        }
    }

    private void GlobalWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F4 && Keyboard.Modifiers == ModifierKeys.Alt)
        {
            e.Handled = true;
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            await Realtime.DisconnectAsync();
        }
        catch
        {
        }

        base.OnExit(e);
    }
}
