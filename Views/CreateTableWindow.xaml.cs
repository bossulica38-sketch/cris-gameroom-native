using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CrisGameRoom.Models;

namespace CrisGameRoom.Views;

public partial class CreateTableWindow : Window
{
    private bool _isPrivate;
    private bool _creating;

    public Room? CreatedRoom { get; private set; }

    public CreateTableWindow() => InitializeComponent();

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadGamesAsync();
        GamesList.Focus();
        Keyboard.Focus(GamesList);
    }

    private async Task LoadGamesAsync()
    {
        GamesList.IsEnabled = false;
        CreateButton.IsEnabled = false;
        StatusText.Text = "Se încarcă jocurile disponibile...";
        try
        {
            var games = await App.Api.GetGamesAsync();
            GamesList.ItemsSource = games.Where(g =>
                string.Equals(g.Lifecycle, "ACTIVE", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(g.Id) && !string.IsNullOrWhiteSpace(g.Name)).ToList();
            GamesList.IsEnabled = GamesList.Items.Count > 0;
            StatusText.Text = GamesList.Items.Count == 0
                ? "Nu există jocuri active disponibile momentan."
                : "Alege jocul pe care vrei să îl deschizi pe tablă.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Jocurile nu au putut fi încărcate: " + ex.Message;
        }
    }

    private void GamesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CreateButton.IsEnabled = GamesList.SelectedItem is Game && !_creating;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _isPrivate = !_isPrivate;
            VisibilityText.Text = _isPrivate
                ? "Tabla va fi privată. Ctrl+H schimbă vizibilitatea în publică."
                : "Tabla va fi publică. Ctrl+H schimbă vizibilitatea în privată.";
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && GamesList.IsKeyboardFocusWithin && GamesList.SelectedItem is Game)
        {
            e.Handled = true;
            _ = CreateSelectedGameAsync();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e) => await CreateSelectedGameAsync();

    private async Task CreateSelectedGameAsync()
    {
        if (_creating) return;
        if (GamesList.SelectedItem is not Game game)
        {
            StatusText.Text = "Selectează un joc din listă.";
            GamesList.Focus();
            return;
        }

        _creating = true;
        CreateButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        GamesList.IsEnabled = false;
        StatusText.Text = "Se creează tabla...";
        try
        {
            CreatedRoom = await App.Api.CreateRoomAsync(
                game.Id, game.Name, visibility: _isPrivate ? "PRIVATE" : "PUBLIC");

            if (!string.Equals(CreatedRoom.Visibility, _isPrivate ? "PRIVATE" : "PUBLIC", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Serverul nu a confirmat vizibilitatea aleasă pentru tablă.");
            if (!string.Equals(CreatedRoom.GameId, game.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("Serverul nu a asociat jocul selectat cu tabla creată.");

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Tabla nu a putut fi creată: " + ex.Message;
            CreatedRoom = null;
            _creating = false;
            GamesList.IsEnabled = true;
            CreateButton.IsEnabled = GamesList.SelectedItem is Game;
            CancelButton.IsEnabled = true;
        }
    }
}
