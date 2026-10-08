using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using CrisGameRoom.Models;

namespace CrisGameRoom.Views;

public partial class MainWindow : Window
{
    private List<Room> _rooms = [];
    private Room? _selectedRoom;

    public MainWindow()
    {
        InitializeComponent();

        App.Realtime.StatusChanged += OnRealtimeStatusChanged;
        App.Realtime.ServerEvent += OnServerEvent;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await TryRestoreSessionAsync();
    }

    private async Task TryRestoreSessionAsync()
    {
        try
        {
            var session = await App.Sessions.LoadAsync();

            if (session is null)
            {
                ConnectionText.Text = "Neautentificat.";
                GeneralStatusText.Text =
                    "Autentificarea este necesară pentru accesarea camerelor.";
                return;
            }

            App.Api.SetAccessToken(session.AccessToken);

            var user = await App.Api.GetCurrentUserAsync();

            await App.Realtime.ConnectAsync(session.AccessToken);

            ConnectionText.Text =
                $"Conectat: {user.DisplayName}";

            LoginButton.IsEnabled = false;
            LogoutButton.IsEnabled = true;

            await LoadRoomsAsync();
        }
        catch (Exception ex)
        {
            await App.Sessions.ClearAsync();
            App.Api.SetAccessToken(null);

            ConnectionText.Text = "Sesiunea nu mai este validă.";
            GeneralStatusText.Text = ex.Message;
        }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var login = new LoginWindow();

        if (login.ShowDialog() != true)
            return;

        await TryRestoreSessionAsync();
    }

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedRoom is not null)
            {
                try
                {
                    await App.Api.LeaveRoomAsync(_selectedRoom.Code);
                }
                catch
                {
                }
            }

            await App.Api.LogoutAsync();
        }
        catch (Exception ex)
        {
            GeneralStatusText.Text = ex.Message;
        }
        finally
        {
            await App.Realtime.DisconnectAsync();
            await App.Sessions.ClearAsync();

            _rooms = [];
            RoomsList.ItemsSource = null;
            _selectedRoom = null;

            LoginButton.IsEnabled = true;
            LogoutButton.IsEnabled = false;
            ConnectionText.Text = "Neautentificat.";
        }
    }

    private async Task LoadRoomsAsync()
    {
        _rooms = await App.Api.GetRoomsAsync();
        RoomsList.ItemsSource = _rooms;

        GeneralStatusText.Text =
            $"Camere publice disponibile: {_rooms.Count}.";
    }

    private async void RoomsList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (RoomsList.SelectedItem is not Room room)
            return;

        _selectedRoom = room;

        RoomNameText.Text = room.Name;
        RoomCodeText.Text = $"Cod cameră: {room.Code}";
        RoomStatusText.Text =
            $"Stare: {room.Status} • Jucători: {room.Players.Count}/{room.MaxPlayers}";

        try
        {
            await App.Api.JoinRoomAsync(room.Code);
            await App.Realtime.JoinRoomAsync(room.Code);

            EventLogText.Text =
                $"Conectat la camera {room.Code}.{Environment.NewLine}" +
                $"Mesaj: {room.Greeting ?? "Nu există mesaj de întâmpinare."}";
        }
        catch (Exception ex)
        {
            EventLogText.Text = ex.Message;
        }
    }

    private void OnRealtimeStatusChanged(string status)
    {
        Dispatcher.Invoke(() =>
        {
            ConnectionText.Text = status;
        });
    }

    private void OnServerEvent(string eventName, JsonElement data)
    {
        Dispatcher.Invoke(() =>
        {
            EventLogText.Text =
                $"[{DateTime.Now:HH:mm:ss}] {eventName}{Environment.NewLine}" +
                data.ToString();
        });
    }
}
