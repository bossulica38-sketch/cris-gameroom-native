using System.Windows;
using System.Windows.Input;
using CrisGameRoom.Models;

namespace CrisGameRoom.Views;

public partial class RoomMenuWindow : Window
{
    public string SelectedAction { get; private set; } = "";
    public bool LeaveRequested => SelectedAction == "leave";
    public bool VisibilityToggleRequested => SelectedAction == "visibility";
    public bool AddBotRequested => SelectedAction == "add-bot";
    public bool RemoveBotRequested => SelectedAction == "remove-bot";
    public bool OptionsRequested => SelectedAction == "options";
    private readonly (string Key, string Label)[] _items =
    [
        ("start", "Pornește jocul"),
        ("options", "Opțiuni"),
        ("add-bot", "Adaugă un bot (B)"),
        ("remove-bot", "Elimină un bot (Shift+B)"),
        ("rules", "Regulile jocului"),
        ("invite", "Invită un jucător"),
        ("kick", "Elimină un jucător"),
        ("ban", "Interzice accesul unui jucător"),
        ("host", "Transferă rolul de gazdă"),
        ("save", "Salvează tabla"),
        ("audio", "Audio"),
        ("visibility", "Schimbă tabla publică / privată (Ctrl+H)"),
        ("friends", "Prieteni"),
        ("spectator", "Spectator (F3)"),
        ("details", "Reguli detaliate"),
        ("moderator", "Moderator"),
        ("blocked", "Lista persoanelor interzise"),
        ("leave", "Părăsește tabla"),
        ("back", "Înapoi la tablă")
    ];

    public RoomMenuWindow(Room room)
    {
        InitializeComponent();
        MenuList.ItemsSource = _items.Select(x => x.Label).ToList();
        MenuList.SelectedIndex = 0;
        MenuList.Focus();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
            return;
        }
        if (e.Key == Key.Enter && MenuList.SelectedIndex >= 0)
        {
            e.Handled = true;
            ActivateSelected();
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => Close();

    private void ActivateSelected()
    {
        if (MenuList.SelectedIndex < 0) return;
        SelectedAction = _items[MenuList.SelectedIndex].Key;
        if (SelectedAction == "back") { Close(); return; }
        if (SelectedAction == "options" || SelectedAction == "add-bot" ||
            SelectedAction == "remove-bot" || SelectedAction == "visibility" ||
            SelectedAction == "leave")
        {
            DialogResult = true;
            Close();
            return;
        }
        MessageBox.Show(this,
            "Această comandă nu este încă legată de funcția serverului. Nu am executat o acțiune fictivă.",
            "Meniul tablei", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
