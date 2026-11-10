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

    public CreateTableWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        NameInput.Focus();
        Keyboard.Focus(NameInput);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _isPrivate = !_isPrivate;
            VisibilityText.Text = _isPrivate ? "PRIVATĂ" : "PUBLICĂ";
            e.Handled = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_creating) return;

        string name = NameInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorText.Text = "Introdu numele tablei.";
            NameInput.Focus();
            return;
        }

        int maxPlayers = 4;
        if (MaxPlayersInput.SelectedItem is ComboBoxItem selected &&
            int.TryParse(selected.Tag?.ToString(), out int parsed))
            maxPlayers = parsed;

        _creating = true;
        CreateButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        ErrorText.Text = string.Empty;

        try
        {
            CreatedRoom = await App.Api.CreateRoomAsync(
                name,
                string.IsNullOrWhiteSpace(DescriptionInput.Text) ? null : DescriptionInput.Text.Trim(),
                visibility: _isPrivate ? "PRIVATE" : "PUBLIC",
                maxPlayers: maxPlayers);

            if (_isPrivate && !string.Equals(CreatedRoom.Visibility, "PRIVATE", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Serverul nu a confirmat crearea tablei private. Tabla nu va fi deschisă.");

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = "Tabla nu a putut fi creată: " + ex.Message;
            CreatedRoom = null;
            _creating = false;
            CreateButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
        }
    }
}
