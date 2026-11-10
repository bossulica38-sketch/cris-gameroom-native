using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CrisGameRoom.Localization;
using CrisGameRoom.Models;

namespace CrisGameRoom.Views;

public partial class JoinTableWindow : Window
{
    private readonly ObservableCollection<RoomListRow> _rows = [];

    public Room? SelectedRoom { get; private set; }

    public JoinTableWindow()
    {
        InitializeComponent();
        RoomsList.ItemsSource = _rows;
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
        Title = LocalizationManager.Get("nativeJoinWindowTitle");
        TitleText.Text = LocalizationManager.Get("nativeJoinWindowTitle");
        SubtitleText.Text = LocalizationManager.Get("nativeJoinWindowSubtitle");
        RefreshButton.Content = LocalizationManager.Get("nativeRoomsRefresh");
        EnterButton.Content = LocalizationManager.Get("nativeRoomEnter");
        BackButton.Content = LocalizationManager.Get("nativeBack");
        System.Windows.Automation.AutomationProperties.SetName(
            RoomsList, LocalizationManager.Get("nativeJoinWindowTitle"));
    }

    private async void JoinTableWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadRoomsAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadRoomsAsync();
    }

    private void RoomsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        EnterButton.IsEnabled = RoomsList.SelectedItem is RoomListRow;
        if (!EnterButton.IsEnabled)
            StatusText.Text = LocalizationManager.Get("nativeSelectRoom");
    }

    private void RoomsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        AcceptSelection();
    }

    private void EnterButton_Click(object sender, RoutedEventArgs e)
    {
        AcceptSelection();
    }

    private void AcceptSelection()
    {
        if (RoomsList.SelectedItem is not RoomListRow selected)
        {
            StatusText.Text = LocalizationManager.Get("nativeSelectRoom");
            return;
        }

        SelectedRoom = selected.Room;
        DialogResult = true;
        Close();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void JoinTableWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter &&
            RoomsList.SelectedItem is RoomListRow)
        {
            e.Handled = true;
            AcceptSelection();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            DialogResult = false;
            Close();
        }
    }

    private async Task LoadRoomsAsync()
    {
        string? selectedCode = (RoomsList.SelectedItem as RoomListRow)?.Room.Code;

        RefreshButton.IsEnabled = false;
        EnterButton.IsEnabled = false;
        StatusText.Text = LocalizationManager.Get("nativeRoomsLoading");

        try
        {
            var rooms = await App.Api.GetRoomsAsync();

            _rows.Clear();

            foreach (var room in rooms
                .Where(r => !string.IsNullOrWhiteSpace(r.Code))
                .Where(r => !string.Equals(r.Status, "CLOSED", StringComparison.OrdinalIgnoreCase))
                .Where(r => !string.Equals(r.Status, "FINISHED", StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                _rows.Add(new RoomListRow(room));
            }

            if (_rows.Count == 0)
            {
                StatusText.Text = LocalizationManager.Get("nativeRoomsEmpty");
            }
            else
            {
                StatusText.Text = $"{_rows.Count}: {LocalizationManager.Get("nativeJoinWindowSubtitle")}";
                if (!string.IsNullOrWhiteSpace(selectedCode))
                {
                    RoomsList.SelectedItem = _rows.FirstOrDefault(
                        x => string.Equals(x.Room.Code, selectedCode, StringComparison.OrdinalIgnoreCase));
                }
            }
        }
        catch
        {
            _rows.Clear();
            StatusText.Text = LocalizationManager.Get("nativeTableLoadError");
        }
        finally
        {
            RefreshButton.IsEnabled = true;
            EnterButton.IsEnabled = RoomsList.SelectedItem is RoomListRow;
        }
    }

    private static string TranslateStatus(string? status)
    {
        return status?.Trim().ToUpperInvariant() switch
        {
            "WAITING" => LocalizationManager.Get("nativeWaiting"),
            "STARTING" => LocalizationManager.Get("nativeStarting"),
            "IN_GAME" => LocalizationManager.Get("nativeInGame"),
            "FINISHED" => LocalizationManager.Get("nativeFinished"),
            "CLOSED" => LocalizationManager.Get("nativeClosed"),
            _ => string.IsNullOrWhiteSpace(status) ? "—" : status
        };
    }

    private static string TranslateVisibility(string? visibility)
    {
        return string.Equals(visibility, "PRIVATE", StringComparison.OrdinalIgnoreCase)
            ? LocalizationManager.Get("nativePrivate")
            : LocalizationManager.Get("nativePublic");
    }

    public sealed class RoomListRow
    {
        public Room Room { get; }
        public string DisplayTitle { get; }
        public string Details { get; }
        public string AccessibleSummary { get; }

        public RoomListRow(Room room)
        {
            Room = room;

            string name = string.IsNullOrWhiteSpace(room.Name)
                ? room.Code
                : room.Name;

            string game = string.IsNullOrWhiteSpace(room.GameId)
                ? LocalizationManager.Get("nativeUnknownGame")
                : room.GameId;

            int playerCount = room.Players?.Count ?? 0;
            string maximum = room.MaxPlayers > 0 ? room.MaxPlayers.ToString() : "—";
            string status = TranslateStatus(room.Status);
            string visibility = TranslateVisibility(room.Visibility);

            DisplayTitle = name;
            Details =
                $"{LocalizationManager.Get("nativeRoomCode")}: {room.Code}  •  " +
                $"{LocalizationManager.Get("nativeRoomGame")}: {game}  •  " +
                $"{LocalizationManager.Get("nativeRoomStatus")}: {status}  •  " +
                $"{LocalizationManager.Get("nativeRoomVisibility")}: {visibility}  •  " +
                $"{LocalizationManager.Get("nativeRoomPlayers")}: {playerCount}/{maximum}";

            AccessibleSummary = $"{DisplayTitle}. {Details}";
        }
    }
}
