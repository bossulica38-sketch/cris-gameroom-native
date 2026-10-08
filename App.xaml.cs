using System.Windows;
using CrisGameRoom.Services.Api;
using CrisGameRoom.Services.Authentication;
using CrisGameRoom.Services.Realtime;

namespace CrisGameRoom;

public partial class App : Application
{
    public static GameRoomApiClient Api { get; } = new();
    public static SessionStore Sessions { get; } = new();
    public static GameRoomRealtimeClient Realtime { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            try
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "CrisGameRoom",
                        "crash.log"),
                    $"[{DateTime.Now:O}] {args.Exception}{Environment.NewLine}{Environment.NewLine}");
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

        var login = new Views.LoginWindow();
        MainWindow = login;

        if (login.ShowDialog() == true)
        {
            var window = new Views.MainWindow();
            MainWindow = window;
            window.Show();
        }
        else
        {
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
