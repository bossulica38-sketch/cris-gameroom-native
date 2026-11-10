using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using CrisGameRoom.Localization;
using CrisGameRoom.Models;
using System.Linq;

namespace CrisGameRoom.Views;

public partial class RoomTableWindow : Window
{
    private Room _room;
    private bool _allowClose;
    private bool _leaving;
    private bool _botActionInProgress;

    public RoomTableWindow(Room room)
    {
        InitializeComponent();
        _room = room;
        ApplyLanguage();
        LocalizationManager.LanguageChanged += LocalizationManager_LanguageChanged;
    }

    private void LocalizationManager_LanguageChanged(object? sender, EventArgs e) => ApplyLanguage();

    private void ApplyLanguage()
    {
        Title = LocalizationManager.Get("nativeTableWindowTitle");
        BotLevelLabel.Text = LocalizationManager.Get("nativeBotLevel");
        BotLevelCombo.ToolTip = LocalizationManager.Get("nativeBotLevel");
        AddBotButton.Content = LocalizationManager.Get("nativeAddBot");
        RemoveBotButton.Content = LocalizationManager.Get("nativeRemoveBot");
        PlayersGroup.Header = LocalizationManager.Get("nativeTablePlayers");
        ChatGroup.Header = LocalizationManager.Get("nativeTableChat");
        ChatInputTextBox.ToolTip = LocalizationManager.Get("nativeChatPlaceholder");
        ChatInputTextBox.Tag = LocalizationManager.Get("nativeChatPlaceholder");
        SendChatButton.Content = LocalizationManager.Get("nativeChatSend");
        RefreshRoomButton.Content = LocalizationManager.Get("nativeTableRefresh");
        LeaveRoomButton.Content = LocalizationManager.Get("nativeLeaveTable");
        UpdateRoomDisplay();
    }

    private async void RoomTableWindow_Loaded(object sender, RoutedEventArgs e)
    {
        App.Realtime.ServerEvent += Realtime_ServerEvent;

        await LoadBotLevelsAsync();
        await RefreshRoomAsync(false);

        try
        {
            await App.Realtime.RequestRoomStateAsync();
            await App.Realtime.ListChatAsync(_room.Code);
        }
        catch
        {
            LiveStatusText.Text = LocalizationManager.Get("nativeChatHistoryFailed");
        }

        ChatInputTextBox.Focus();
    }

    private void RoomTableWindow_Closed(object? sender, EventArgs e)
    {
        App.Realtime.ServerEvent -= Realtime_ServerEvent;
        LocalizationManager.LanguageChanged -= LocalizationManager_LanguageChanged;
    }

    private async void RefreshRoomButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshRoomAsync(true);
        try
        {
            await App.Realtime.ListChatAsync(_room.Code);
            await App.Realtime.RequestRoomStateAsync();
        }
        catch
        {
            LiveStatusText.Text = LocalizationManager.Get("nativeChatHistoryFailed");
        }
    }

    private async Task RefreshRoomAsync(bool showFeedback)
    {
        try
        {
            var current = await App.Api.GetRoomAsync(_room.Code);
            _room = current;
            UpdateRoomDisplay();

            if (showFeedback)
                LiveStatusText.Text = LocalizationManager.Get("nativeRoomUpdated");
        }
        catch
        {
            RoomStatusText.Text = LocalizationManager.Get("nativeTableLoadError");
            if (showFeedback)
                LiveStatusText.Text = LocalizationManager.Get("nativeTableLoadError");
        }
    }

    private void UpdateRoomDisplay()
    {
        RoomNameText.Text = string.IsNullOrWhiteSpace(_room.Name)
            ? LocalizationManager.Get("nativeTableWindowTitle")
            : _room.Name;

        string game = string.IsNullOrWhiteSpace(_room.GameId)
            ? LocalizationManager.Get("nativeUnknownGame")
            : _room.GameId;

        string visibility = string.Equals(_room.Visibility, "PRIVATE",
                StringComparison.OrdinalIgnoreCase)
            ? LocalizationManager.Get("nativePrivate")
            : LocalizationManager.Get("nativePublic");

        string status = _room.Status?.Trim().ToUpperInvariant() switch
        {
            "WAITING" => LocalizationManager.Get("nativeWaiting"),
            "STARTING" => LocalizationManager.Get("nativeStarting"),
            "IN_GAME" => LocalizationManager.Get("nativeInGame"),
            "FINISHED" => LocalizationManager.Get("nativeFinished"),
            "CLOSED" => LocalizationManager.Get("nativeClosed"),
            _ => string.IsNullOrWhiteSpace(_room.Status) ? "—" : _room.Status
        };

        var players = _room.Players ?? [];
        var bots = _room.Bots ?? [];
        int count = players.Count + bots.Count;
        string max = _room.MaxPlayers > 0 ? _room.MaxPlayers.ToString() : "—";

        RoomSummaryText.Text =
            $"{LocalizationManager.Get("nativeRoomCode")}: {_room.Code}  •  " +
            $"{LocalizationManager.Get("nativeUnknownGame")}: {game}  •  " +
            $"{LocalizationManager.Get("nativeTableVisibility")}: {visibility}  •  " +
            $"{LocalizationManager.Get("nativeTablePlayers")}: {count}/{max}";

        RoomStatusText.Text = $" {LocalizationManager.Get("nativeTableStatus")}: {status}";

        var roster = players.Select(p =>
        {
            string name = !string.IsNullOrWhiteSpace(p.DisplayName)
                ? p.DisplayName
                : !string.IsNullOrWhiteSpace(p.Username)
                    ? p.Username
                    : p.UserId;

            string playerStatus = p.Status?.Trim().ToUpperInvariant() switch
            {
                "PLAYING" => LocalizationManager.Get("nativeInGame"),
                "READY" => LocalizationManager.Get("nativeStarting"),
                "SPECTATOR" => "Spectator",
                "JOINED" => "Conectat",
                "DISCONNECTED" => "Deconectat",
                "LEFT" => "A părăsit tabla",
                _ => ""
            };

            return string.IsNullOrWhiteSpace(playerStatus)
                ? name
                : $"{name} — {playerStatus}";
        }).ToList();

        roster.AddRange(bots.Select(bot =>
            $"{bot.Name} — Adversar AI, nivel {bot.Level}: {bot.LevelName}. Stil: {bot.Archetype}"));

        PlayersList.ItemsSource = roster.Count == 0
            ? new[] { LocalizationManager.Get("nativeNoPlayers") }
            : roster;
    }


    private async Task LoadBotLevelsAsync()
    {
        try
        {
            var levels = await App.Api.GetBotLevelsAsync();
            BotLevelCombo.ItemsSource = levels;
            BotLevelCombo.DisplayMemberPath = nameof(OpponentDifficultyLevel.DisplayName);
            BotLevelCombo.SelectedValuePath = nameof(OpponentDifficultyLevel.Level);
            BotLevelCombo.SelectedValue = levels.Any(x => x.Level == 1)
                ? 1
                : levels.FirstOrDefault()?.Level;

            bool ready = levels.Count > 0;
            BotLevelCombo.IsEnabled = ready;
            AddBotButton.IsEnabled = ready;

            if (!ready)
            {
                LiveStatusText.Text =
                    LocalizationManager.Get("nativeBotNoLevels");
            }
        }
        catch (Exception ex)
        {
            BotLevelCombo.IsEnabled = false;
            AddBotButton.IsEnabled = false;
            LiveStatusText.Text =
                $"Nu s-au putut încărca nivelurile AI din server: {ex.Message}";
        }
    }

    private async void AddBotButton_Click(object sender, RoutedEventArgs e)
    {
        await AddBotToRoomAsync();
    }

    private async Task AddBotToRoomAsync()
    {
        if (_botActionInProgress || _leaving)
            return;

        if (BotLevelCombo.SelectedValue is not int level)
        {
            LiveStatusText.Text = LocalizationManager.Get("nativeBotSelectLevel");
            return;
        }

        _botActionInProgress = true;
        AddBotButton.IsEnabled = false;
        RemoveBotButton.IsEnabled = false;

        try
        {
            var bot = await App.Api.AddBotAsync(_room.Code, level);
            await RefreshRoomAsync(false);

            LiveStatusText.Text = string.IsNullOrWhiteSpace(_room.GameId)
                ? $"{bot.Name}. {LocalizationManager.Get("nativeBotAdded")} Nivel {bot.Level} — {bot.LevelName}. " +
                  "Atenție: camera nu are un joc asociat în baza de date; mutările automate nu sunt active."
                : $"{bot.Name}. {LocalizationManager.Get("nativeBotAdded")} Nivel {bot.Level} — {bot.LevelName}.";
        }
        catch (Exception ex)
        {
            LiveStatusText.Text = $"Adăugarea adversarului AI a eșuat: {ex.Message}";
        }
        finally
        {
            _botActionInProgress = false;
            AddBotButton.IsEnabled = BotLevelCombo.Items.Count > 0;
            RemoveBotButton.IsEnabled = true;
            ChatInputTextBox.Focus();
        }
    }

    private async void RemoveBotButton_Click(object sender, RoutedEventArgs e)
    {
        await RemoveLastBotAsync();
    }

    private async Task RemoveLastBotAsync()
    {
        if (_botActionInProgress || _leaving)
            return;

        var bot = (_room.Bots ?? []).LastOrDefault();
        if (bot is null)
        {
            LiveStatusText.Text = LocalizationManager.Get("nativeBotNone");
            return;
        }

        _botActionInProgress = true;
        AddBotButton.IsEnabled = false;
        RemoveBotButton.IsEnabled = false;

        try
        {
            await App.Api.RemoveBotAsync(_room.Code, bot.Id);
            await RefreshRoomAsync(false);
            LiveStatusText.Text = $"{bot.Name}. {LocalizationManager.Get("nativeBotRemoved")}";
        }
        catch (Exception ex)
        {
            LiveStatusText.Text = $"Eliminarea adversarului AI a eșuat: {ex.Message}";
        }
        finally
        {
            _botActionInProgress = false;
            AddBotButton.IsEnabled = BotLevelCombo.Items.Count > 0;
            RemoveBotButton.IsEnabled = true;
            ChatInputTextBox.Focus();
        }
    }

    private async void SendChatButton_Click(object sender, RoutedEventArgs e)
    {
        await SendChatAsync();
    }

    private async Task SendChatAsync()
    {
        string text = ChatInputTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        SendChatButton.IsEnabled = false;
        try
        {
            await App.Realtime.SendChatAsync(_room.Code, text);
            ChatInputTextBox.Clear();
            LiveStatusText.Text = LocalizationManager.Get("nativeChatSent");
        }
        catch
        {
            LiveStatusText.Text = LocalizationManager.Get("nativeJoinRoomError");
        }
        finally
        {
            SendChatButton.IsEnabled = true;
            ChatInputTextBox.Focus();
        }
    }

    private void Realtime_ServerEvent(string eventName, JsonElement payload)
    {
        JsonElement safePayload;
        try
        {
            safePayload = payload.Clone();
        }
        catch
        {
            return;
        }

        Dispatcher.BeginInvoke(new Action(() =>
        {
            _ = HandleRealtimeEventAsync(eventName, safePayload);
        }));
    }

    private async Task HandleRealtimeEventAsync(string eventName, JsonElement payload)
    {
        switch (eventName)
        {
            case "room:chat:list":
                ShowChatHistory(payload);
                break;

            case "room.message_created":
                var message = FormatChatLine(payload);
                if (!string.IsNullOrWhiteSpace(message))
                    AppendChatLine(message);
                break;

            case "room.participant_joined":
                AppendChatLine(LocalizationManager.Get("nativePlayerJoined"));
                await RefreshRoomAsync(false);
                break;

            case "room.participant_left":
                AppendChatLine(LocalizationManager.Get("nativePlayerLeft"));
                await RefreshRoomAsync(false);
                break;

            case "room.updated":
            case "room:state":
            case "room:joined":
            case "room.owner_changed":
                await RefreshRoomAsync(false);
                break;

            case "room:chat:delete":
                try
                {
                    await App.Realtime.ListChatAsync(_room.Code);
                }
                catch
                {
                    LiveStatusText.Text = LocalizationManager.Get("nativeChatHistoryFailed");
                }
                break;
        }
    }

    private void ShowChatHistory(JsonElement payload)
    {
        var lines = ExtractChatLines(payload);

        if (lines.Count == 0)
        {
            if (payload.ValueKind == JsonValueKind.Array && payload.GetArrayLength() == 0)
            {
                ChatHistoryTextBox.Clear();
                AppendChatLine(LocalizationManager.Get("nativeChatHistoryEmpty"));
            }
            else
            {
                LiveStatusText.Text = LocalizationManager.Get("nativeChatHistoryFailed");
            }

            return;
        }

        ChatHistoryTextBox.Clear();
        foreach (string line in lines)
            AppendChatLine(line);
    }

    private static List<string> ExtractChatLines(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.String)
        {
            string? value = root.GetString();
            return string.IsNullOrWhiteSpace(value) ? [] : [value];
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            var rows = new List<string>();
            foreach (var item in root.EnumerateArray())
            {
                string? line = FormatChatLine(item);
                if (!string.IsNullOrWhiteSpace(line))
                    rows.Add(line);
            }
            return rows;
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name.Equals("messages", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("items", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("data", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("results", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("history", StringComparison.OrdinalIgnoreCase))
                {
                    var found = ExtractChatLines(property.Value);
                    if (found.Count > 0)
                        return found;
                }
            }

            string? line = FormatChatLine(root);
            if (!string.IsNullOrWhiteSpace(line))
                return [line];
        }

        return [];
    }

    private static string? FormatChatLine(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString();

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        string? content = ReadText(element, "content", "text", "body", "messageText", "message");
        if (string.IsNullOrWhiteSpace(content))
            return null;

        string? author = ReadText(element, "username", "displayName", "senderName", "authorName");

        if (string.IsNullOrWhiteSpace(author))
            author = ReadNestedText(element, new[] { "sender", "author", "user", "player" },
                "username", "displayName", "name");

        return string.IsNullOrWhiteSpace(author) ? content : $"{author}: {content}";
    }

    private static string? ReadText(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in element.EnumerateObject())
        {
            if (!names.Any(n => property.Name.Equals(n, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (property.Value.ValueKind == JsonValueKind.String)
                return property.Value.GetString();

            if (property.Value.ValueKind == JsonValueKind.Number ||
                property.Value.ValueKind == JsonValueKind.True ||
                property.Value.ValueKind == JsonValueKind.False)
                return property.Value.ToString();

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                string? nested = ReadText(property.Value, "content", "text", "body", "username",
                    "displayName", "name");
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }

        return null;
    }

    private static string? ReadNestedText(
        JsonElement element, string[] containers, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in element.EnumerateObject())
        {
            if (containers.Any(n => property.Name.Equals(n, StringComparison.OrdinalIgnoreCase)))
            {
                string? value = ReadText(property.Value, names);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return null;
    }

    private void AppendChatLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        if (ChatHistoryTextBox.Text.Length > 0)
            ChatHistoryTextBox.AppendText(Environment.NewLine);

        ChatHistoryTextBox.AppendText(line);
        ChatHistoryTextBox.ScrollToEnd();
    }

    private void RoomTableWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        bool typingOrSelectingLevel =
            ChatInputTextBox.IsKeyboardFocusWithin ||
            BotLevelCombo.IsKeyboardFocusWithin;

        if (!typingOrSelectingLevel &&
            e.Key == Key.B &&
            Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = true;
            _ = AddBotToRoomAsync();
            return;
        }

        if (!typingOrSelectingLevel &&
            e.Key == Key.B &&
            Keyboard.Modifiers == ModifierKeys.Shift)
        {
            e.Handled = true;
            _ = RemoveLastBotAsync();
            return;
        }

        if (e.Key == Key.I &&
            Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            PlayersList.Focus();
            if (PlayersList.Items.Count > 0)
                PlayersList.SelectedIndex = 0;
            return;
        }

        if (e.Key == Key.Q &&
            Keyboard.Modifiers == ModifierKeys.None &&
            !ChatInputTextBox.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            _ = LeaveTableAsync(true);
        }
    }

    private void LeaveRoomButton_Click(object sender, RoutedEventArgs e)
    {
        _ = LeaveTableAsync(true);
    }

    private async Task LeaveTableAsync(bool confirm)
    {
        if (_leaving)
            return;

        if (confirm)
        {
            var result = MessageBox.Show(
                LocalizationManager.Get("nativeLeaveTableQuestion"),
                LocalizationManager.Get("nativeLeaveTableConfirm"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
        }

        _leaving = true;
        LeaveRoomButton.IsEnabled = false;
        RefreshRoomButton.IsEnabled = false;

        try
        {
            await App.Api.LeaveRoomAsync(_room.Code);
        }
        catch
        {
            _leaving = false;
            LeaveRoomButton.IsEnabled = true;
            RefreshRoomButton.IsEnabled = true;

            MessageBox.Show(
                LocalizationManager.Get("nativeLeaveTableError"),
                LocalizationManager.Get("nativeLeaveTable"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        try
        {
            await App.Realtime.LeaveRoomAsync(_room.Code);
        }
        catch
        {
            // API-ul a confirmat părăsirea tablei; închiderea rămâne permisă.
        }

        _allowClose = true;
        Close();
    }

    private void RoomTableWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;
        _ = LeaveTableAsync(true);
    }

    private async void ChatInputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = true;
            await SendChatAsync();
        }
    }
}
