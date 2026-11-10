using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CrisGameRoom.Services.Updates;
using CrisGameRoom.Localization;

namespace CrisGameRoom.Views;

public partial class PlatformShellWindow : Window
{
    private double _textScale = 1.0;
    private bool _loaded;
    private bool _logoutInProgress;

    public PlatformShellWindow()
    {
        InitializeComponent();

        VersionText.Text =
            $"Versiunea {typeof(PlatformShellWindow).Assembly.GetName().Version}";

        NavigationMenu.PreviewKeyDown += NavigationMenu_PreviewKeyDown;
        NavigationMenu.MouseDoubleClick += NavigationMenu_MouseDoubleClick;
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
        Title = "CRIS GAMEROOM — " + LocalizationManager.Get("nativePlatformMenu");
        var labels = new Dictionary<string, string>
        {
            ["create"] = "nativeMenuCreate", ["join"] = "nativeMenuJoin",
            ["restore"] = "nativeMenuRestore", ["options"] = "nativeOptions",
            ["features"] = "nativeMenuFeatures", ["leave"] = "nativeMenuLeave"
        };
        foreach (ListBoxItem item in NavigationMenu.Items.OfType<ListBoxItem>())
        {
            string tag = item.Tag?.ToString() ?? "";
            if (labels.TryGetValue(tag, out string? key))
            {
                item.Content = LocalizationManager.Get(key);
                AutomationProperties.SetName(item, LocalizationManager.Get(key));
            }
        }
        VersionText.Text = $"{LocalizationManager.Get("nativeVersion")} {typeof(PlatformShellWindow).Assembly.GetName().Version}";
        RenderSelectedItem();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded)
            return;

        _loaded = true;

        if (NavigationMenu.SelectedIndex < 0)
            NavigationMenu.SelectedIndex = 0;

        RenderSelectedItem();
        NavigationMenu.Focus();
        Keyboard.Focus(NavigationMenu);

        await RefreshConnectionStatusAsync();
    }

    private void NavigationMenu_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        RenderSelectedItem();
    }

    private void NavigationMenu_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Space)
        {
            e.Handled = true;
            _ = ActivateSelectedItemAsync();
        }
    }

    private void NavigationMenu_MouseDoubleClick(
        object sender,
        MouseButtonEventArgs e)
    {
        if (NavigationMenu.SelectedItem is ListBoxItem)
            _ = ActivateSelectedItemAsync();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            NavigationMenu.Focus();
            Keyboard.Focus(NavigationMenu);
            e.Handled = true;
            return;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (e.Key == Key.Add || e.Key == Key.OemPlus || e.Key == Key.Subtract && false)
            {
                AdjustTextScale(0.1);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
            {
                AdjustTextScale(-0.1);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.D0 || e.Key == Key.NumPad0)
            {
                _textScale = 1.0;
                ApplyTextScale();
                e.Handled = true;
                return;
            }
        }
    }

    private string SelectedKey =>
        (NavigationMenu.SelectedItem as ListBoxItem)?.Tag?.ToString() ?? "create";

    private void RenderSelectedItem()
    {
        if (PageTitle is null || PageDescription is null || PageBody is null)
            return;

        PageBody.Children.Clear();

        string title = SelectedKey switch
        {
            "create" => LocalizationManager.Get("nativeMenuCreate"),
            "join" => LocalizationManager.Get("nativeMenuJoin"),
            "restore" => LocalizationManager.Get("nativeMenuRestore"),
            "options" => LocalizationManager.Get("nativeOptions"),
            "features" => LocalizationManager.Get("nativeMenuFeatures"),
            "leave" => LocalizationManager.Get("nativeMenuLeave"),
            _ => LocalizationManager.Get("nativeMainMenu")
        };

        RenderPage(
            title,
            LocalizationManager.Get("nativeMainDescription"));

        ApplyTextScale();
    }

    private void RenderPage(
        string title,
        string description,
        params string[] lines)
    {
        PageTitle.Text = title;
        PageDescription.Text = description;

        foreach (string line in lines)
        {
            var text = new TextBlock
            {
                Text = line,
                FontSize = 15,
                Foreground = TryFindResource("ThemeTextBrush") as Brush ?? Brushes.Black,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12),
                Tag = 15.0
            };
            AutomationProperties.SetName(text, line);
            PageBody.Children.Add(text);
        }
    }

    private void RenderProfilePage()
    {
        PageBody.Children.Clear();
        PageTitle.Text = "Profil și sesiune";
        PageDescription.Text =
            "Datele afișate aici provin din obiectul de sesiune păstrat de client. Nu sunt inventate date de profil.";

        object? sessions = GetStaticProperty(typeof(App), "Sessions");

        if (sessions is null)
        {
            AddBodyLine("Sesiunea nu este disponibilă în client.");
            return;
        }

        bool any = false;
        foreach (PropertyInfo property in sessions.GetType().GetProperties(
            BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length != 0 ||
                property.PropertyType != typeof(string))
                continue;

            string name = property.Name;
            if (Regex.IsMatch(name, "token|secret|password|credential|refresh|access", RegexOptions.IgnoreCase))
                continue;

            if (!Regex.IsMatch(name, "user|name|email|display|profile|id", RegexOptions.IgnoreCase))
                continue;

            try
            {
                object? value = property.GetValue(sessions);
                if (value is string s && !string.IsNullOrWhiteSpace(s))
                {
                    AddBodyLine($"{name}: {s}");
                    any = true;
                }
            }
            catch
            {
            }
        }

        if (!any)
        {
            AddBodyLine("Sesiunea este gestionată de client. Profilul complet trebuie furnizat prin endpointul de profil al serverului.");
        }
    }

    private void AddBodyLine(string line)
    {
        var block = new TextBlock
        {
            Text = line,
            FontSize = 15,
            Foreground = TryFindResource("ThemeTextBrush") as Brush ?? Brushes.Black,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12),
            Tag = 15.0
        };

        PageBody.Children.Add(block);
        ApplyTextScale();
    }

    private async Task ActivateSelectedItemAsync()
    {
        if (IsEnabled == false || _logoutInProgress)
            return;

        switch (SelectedKey)
        {
            case "options":
                new OptionsWindow { Owner = this }.ShowDialog();
                ApplyLanguage();
                break;
            case "restore":
                MessageBox.Show(this, LocalizationManager.Get("nativeNoSavedTables"), LocalizationManager.Get("nativeMenuRestore"), MessageBoxButton.OK, MessageBoxImage.Information);
                break;
            case "create":
                OpenRoomWindow(true);
                break;
            case "join":
                OpenRoomWindow(false);
                break;
            default:
                RenderSelectedItem();
                break;
        }

        NavigationMenu.Focus();
        Keyboard.Focus(NavigationMenu);
    }

    private void OpenRoomWindow(bool triggerCreateAction)
    {
        var window = new MainWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        if (triggerCreateAction)
        {
            window.Loaded += (_, _) =>
            {
                Dispatcher.BeginInvoke(
                    DispatcherPriority.ApplicationIdle,
                    new Action(() =>
                    {
                        Button? createButton = FindVisualChildren<Button>(window)
                            .FirstOrDefault(IsCreateTableCommand);

                        if (createButton is null)
                        {
                            ConnectionStatusText.Text =
                                "Fereastra camerelor s-a deschis; comanda de creare nu a putut fi identificată automat.";

                            MessageBox.Show(
                                this,
                                "Fereastra camerelor a fost deschisă, dar nu a fost găsit un element de creare identificabil automat.\n\nComanda nu a fost simulată. Următorul pas este conectarea directă a meniului la metoda reală de creare.",
                                "Creează o tablă nouă",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            return;
                        }

                        if (!createButton.IsEnabled)
                        {
                            ConnectionStatusText.Text =
                                "Comanda de creare este momentan dezactivată.";
                            return;
                        }

                        createButton.RaiseEvent(
                            new RoutedEventArgs(Button.ClickEvent, createButton));

                        ConnectionStatusText.Text =
                            "Comanda existentă de creare a fost transmisă din meniul principal.";
                    }));
            };
        }

        try
        {
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                "Fereastra camerelor nu a putut fi deschisă.\n\n" + ex.Message,
                "CRIS GAMEROOM",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            NavigationMenu.Focus();
            Keyboard.Focus(NavigationMenu);
        }
    }

    private void OpenChildWindow(string typeName)
    {
        try
        {
            Type? type = typeof(App).Assembly.GetType(typeName);
            if (type is null || !typeof(Window).IsAssignableFrom(type))
                throw new InvalidOperationException(
                    "Fereastra solicitată nu există în aplicație: " + typeName);

            if (Activator.CreateInstance(type) is not Window window)
                throw new InvalidOperationException(
                    "Fereastra nu a putut fi inițializată: " + typeName);

            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                "Fereastra nu a putut fi deschisă.\n\n" + ex.Message,
                "CRIS GAMEROOM",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            NavigationMenu.Focus();
            Keyboard.Focus(NavigationMenu);
        }
    }

    private static bool IsCreateTableCommand(Button button)
    {
        string content = ExtractContent(button.Content);
        string accessibleName = AutomationProperties.GetName(button) ?? "";
        string full = string.Join(
            " ",
            button.Name ?? "",
            content,
            accessibleName);

        return Regex.IsMatch(
            full,
            @"(cre[a-zăâîșț]*.{0,45}(tabl|camer|room))|" +
            @"(create.{0,45}(table|room|game))|" +
            @"(tabl[aă].{0,20}nou[aă])|" +
            @"(new.{0,20}table)",
            RegexOptions.IgnoreCase);
    }

    private static string ExtractContent(object? content)
    {
        if (content is null)
            return "";

        if (content is string value)
            return value;

        if (content is TextBlock textBlock)
            return textBlock.Text;

        if (content is DependencyObject d)
        {
            return string.Join(
                " ",
                FindVisualChildren<TextBlock>(d).Select(x => x.Text));
        }

        return content.ToString() ?? "";
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        if (parent is null)
            yield break;

        int count;
        try
        {
            count = VisualTreeHelper.GetChildrenCount(parent);
        }
        catch
        {
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            DependencyObject child;
            try
            {
                child = VisualTreeHelper.GetChild(parent, i);
            }
            catch
            {
                continue;
            }

            if (child is T match)
                yield return match;

            foreach (T descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }

    private void AdjustTextScale(double amount)
    {
        _textScale = Math.Clamp(_textScale + amount, 0.8, 1.8);
        ApplyTextScale();
        ConnectionStatusText.Text =
            $"Dimensiunea textului: {(int)Math.Round(_textScale * 100)}%";
    }

    private void ApplyTextScale()
    {
        if (NavigationMenu is null || PageTitle is null ||
            PageDescription is null || PageBody is null)
            return;

        foreach (ListBoxItem item in FindVisualChildren<ListBoxItem>(NavigationMenu))
            item.FontSize = 15 * _textScale;

        PageTitle.FontSize = 24 * _textScale;
        PageDescription.FontSize = 16 * _textScale;

        foreach (TextBlock block in FindVisualChildren<TextBlock>(PageBody))
        {
            double baseSize = block.Tag is double size ? size : 15.0;
            block.FontSize = baseSize * _textScale;
        }
    }

    private async Task RefreshConnectionStatusAsync()
    {
        try
        {
            object? api = GetStaticProperty(typeof(App), "Api");
            if (api is null)
            {
                ConnectionStatusText.Text = "Clientul API nu este inițializat.";
                return;
            }

            object? rooms = await InvokeMethodAsync(api, "GetRoomsAsync");
            int? count = CountItems(rooms, new HashSet<object>());

            ConnectionStatusText.Text = count.HasValue
                ? $"Server: conectat · Camere primite: {count.Value}"
                : "Server: răspuns primit";
        }
        catch (Exception ex)
        {
            ConnectionStatusText.Text = "Server: conexiunea nu a putut fi confirmată.";
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private static int? CountItems(object? value, HashSet<object> visited)
    {
        if (value is null || value is string)
            return null;

        if (value is ICollection collection)
            return collection.Count;

        Type type = value.GetType();
        if (!type.IsValueType && !visited.Add(value))
            return null;

        if (value is IEnumerable enumerable)
        {
            int count = 0;
            foreach (object? _ in enumerable)
                count++;
            return count;
        }

        foreach (string propertyName in new[]
        {
            "Rooms", "Items", "Data", "Results", "Values"
        })
        {
            try
            {
                PropertyInfo? property = type.GetProperty(propertyName);
                object? nested = property?.GetValue(value);
                int? count = CountItems(nested, visited);
                if (count.HasValue)
                    return count;
            }
            catch
            {
            }
        }

        return null;
    }

    private async Task LogoutAsync()
    {
        MessageBoxResult choice = MessageBox.Show(
            this,
            "Dorești să te deconectezi din CRIS GAMEROOM?",
            "Deconectare",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (choice != MessageBoxResult.Yes)
            return;

        _logoutInProgress = true;
        ConnectionStatusText.Text = "Se închide sesiunea...";

        try
        {
            object? api = GetStaticProperty(typeof(App), "Api");
            if (api is not null)
            {
                try
                {
                    await InvokeMethodAsync(api, "LogoutAsync");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Logout API: " + ex);
                }
            }

            object? sessions = GetStaticProperty(typeof(App), "Sessions");
            if (sessions is not null)
            {
                foreach (string methodName in new[]
                {
                    "Clear", "ClearSession", "Reset", "ForgetSession"
                })
                {
                    MethodInfo? method = sessions.GetType().GetMethods(
                        BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(m =>
                            m.Name == methodName &&
                            m.GetParameters().Length == 0);

                    if (method is null)
                        continue;

                    try
                    {
                        object? result = method.Invoke(sessions, null);
                        if (result is Task task)
                            await task;
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "Curățare sesiune: " + ex);
                    }
                }
            }
        }
        finally
        {
            Application.Current.Shutdown();
        }
    }

    private static object? GetStaticProperty(Type type, string name)
    {
        try
        {
            return type.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(null);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<object?> InvokeMethodAsync(
        object instance,
        string methodName)
    {
        MethodInfo? method = instance.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == methodName)
            .FirstOrDefault(m =>
                m.GetParameters().All(p =>
                    p.IsOptional ||
                    p.ParameterType == typeof(CancellationToken)));

        if (method is null)
        {
            method = instance.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name == methodName &&
                    m.GetParameters().Length == 0);
        }

        if (method is null)
            throw new MissingMethodException(
                instance.GetType().FullName, methodName);

        var parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(CancellationToken))
                arguments[i] = CancellationToken.None;
            else if (parameters[i].IsOptional)
                arguments[i] = parameters[i].DefaultValue;
            else
                throw new InvalidOperationException(
                    "Metoda are parametri obligatorii: " + methodName);
        }

        object? result = method.Invoke(instance, arguments);
        if (result is not Task task)
            return result;

        await task.ConfigureAwait(true);

        PropertyInfo? resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }
}
