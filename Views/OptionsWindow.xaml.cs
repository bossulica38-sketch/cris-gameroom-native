using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CrisGameRoom.Views;

public partial class OptionsWindow : Window
{
    private static readonly string SettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrisGameRoom", "options.json");
    private readonly Dictionary<string, object?> values = new(StringComparer.OrdinalIgnoreCase);
    private bool building;
    private string currentPage = "general";
    private static readonly string[] Channels = ["Mesaje de sistem","Mesaje de joc","Chat public","Mesaje private","Invitații","Mesaje ale tablei","Mesaje globale","Notificări","Prieteni","Administrare"];
    private static readonly string[] Views = ["General","Joc","Discuții","Mesaje private","Invitații","Prieteni","Chat public"];
    private static readonly string[] Palette = ["Negru","Violet","Magenta","Pink","Red","Brown","Orange","Yellow","Green 1","Green 2","Azure","Blue 1","Blue 2","Gray 1","Gray 2","Blue 3"];

    public OptionsWindow()
    {
        InitializeComponent();
        LoadValues();
        ApplyMenuLanguage();
        LocalizationManager.LanguageChanged += LocalizationManager_LanguageChanged;
    }

    private void LocalizationManager_LanguageChanged(object? sender, EventArgs e)
    {
        ApplyMenuLanguage();
        if (IsLoaded) ShowPage(currentPage);
    }

    private void ApplyMenuLanguage()
    {
        Title = LocalizationManager.Get("nativeOptions");
        if (OptionsMenu.Items.Count < 8) return;
        string[] keys = ["nativeLanguage", "nativeGeneral", "nativeAudio", "nativeReading", "nativeSecurity", "nativePrivacy", "nativeDefaults", "nativeBack"];
        for (int i = 0; i < keys.Length; i++)
            if (OptionsMenu.Items[i] is ListBoxItem item)
            {
                item.Content = LocalizationManager.Get(keys[i]);
                AutomationProperties.SetName(item, LocalizationManager.Get(keys[i]));
            }
    }

    protected override void OnClosed(EventArgs e)
    {
        LocalizationManager.LanguageChanged -= LocalizationManager_LanguageChanged;
        base.OnClosed(e);
    }
    private void Window_Loaded(object sender, RoutedEventArgs e) { OptionsMenu.SelectedIndex = 1; OptionsMenu.Focus(); Keyboard.Focus(OptionsMenu); }
    private void LoadValues()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(SettingsFile));
                if (data != null) foreach (var p in data)
                    values[p.Key] = p.Value.ValueKind switch
                    {
                        JsonValueKind.True => true, JsonValueKind.False => false,
                        JsonValueKind.Number when p.Value.TryGetDouble(out var n) => n,
                        JsonValueKind.String => p.Value.GetString(), _ => null
                    };
            }
        }
        catch { }
        Default("confirmExit",true); Default("maximize",true); Default("fullscreen",false);
        Default("minimize",true); Default("autoLogin",true); Default("history",true);
        Default("audioOutput","Automat"); Default("notificationVolume",100d);
        Default("notificationOutput","Automat"); Default("musicVolume",100d);
        Default("musicOutput","Automat"); Default("arcadeSounds",true);
        Default("disableInterfaceSounds",true); Default("directSounds",true);
        Default("profile","Implicit"); Default("visualAuto",true); Default("tooltips",true);
        Default("tooltipDelay",1.5d); Default("announceExtra",true); Default("announceDelay",2d);
        Default("embedded",true); Default("language",LocalizationManager.CurrentLanguage);
        foreach (var c in Channels) Default("channel:"+c,true);
        foreach (var v in Views) Default("view:"+v,true);
    }
    private void Default(string key, object value) { if (!values.ContainsKey(key)) values[key]=value; }
    private void Save()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!); File.WriteAllText(SettingsFile,JsonSerializer.Serialize(values,new JsonSerializerOptions{WriteIndented=true})); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }
    private void Put(string key, object? value) { if (building) return; values[key]=value; Save(); }
    private bool Bool(string key) => values.TryGetValue(key,out var v) && v is bool b && b;
    private string Str(string key,string fallback="") => values.TryGetValue(key,out var v) && v is string s ? s : fallback;
    private double Num(string key) => values.TryGetValue(key,out var v) && v is double d ? d : v is int i ? i : 0;

    private void OptionsMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (building || OptionsMenu.SelectedItem is not ListBoxItem item) return;
        var page=item.Tag?.ToString() ?? "general";
        if (page=="back") { Close(); return; }
        if (page=="defaults")
        {
            if (MessageBox.Show(this,"Restabilești valorile implicite ale opțiunilor?","Confirmare",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)
            { values.Clear(); LoadValues(); Save(); ShowPage("general"); }
            return;
        }
        ShowPage(page);
    }

    private void ShowPage(string page)
    {
        currentPage=page; SettingsBody.Children.Clear(); building=true;
        try
        {
            switch(page)
            {
                case "language":
                    Header(LocalizationManager.Get("nativeLanguage"),LocalizationManager.Get("nativeLanguageRestart"));
                    LanguageCombo(); Back(); break;
                case "general":
                    Header("Opțiuni generale","Preferințele generale ale aplicației.");
                    Check("Confirmă ieșirea","confirmExit"); Check("Maximizează fereastra la deschidere","maximize");
                    Check("Deschide fereastra pe tot ecranul","fullscreen"); Check("Minimizează în bara de activități","minimize");
                    Check("Autentificare automată la pornire","autoLogin"); Check("Salvează automat istoricul","history");
                    Text("Salvează istoricul în","historyPath"); Back(); break;
                case "audio":
                    Header("Audio și text-to-speech","Volumul și ieșirile audio ale aplicației.");
                    Combo("Ieșire audio generală","audioOutput",["Automat"]);
                    SliderControl("Volum notificări","notificationVolume",0,100,"%");
                    Combo("Ieșire audio notificări","notificationOutput",["Automat"]);
                    SliderControl("Volum fluxuri muzicale","musicVolume",0,100,"%");
                    Combo("Ieșire audio fluxuri muzicale","musicOutput",["Automat"]);
                    Check("Redă sunetele notificărilor când modul Arcade este activ","arcadeSounds");
                    Check("Dezactivează sunetele interfeței","disableInterfaceSounds");
                    Check("Folosește sunete directe în locul sistemului audio","directSounds");
                    Note("Preferința pentru sunete directe este salvată; conectarea ei la motorul audio necesită verificare."); Back(); break;
                case "reading":
                    Header("Citire și aspect","Personalizează anunțurile, vizualizarea și aspectul interfeței.");
                    Combo("Profil","profile",["Implicit"]);
                    Button("Canale",()=>ShowPage("channels")); Button("Vizualizări",()=>ShowPage("views"));
                    Button("Culori",()=>ShowPage("colors")); Button("Font",()=>ShowPage("fonts"));
                    Check("Temă vizuală automată","visualAuto"); Check("Activează explicațiile contextuale","tooltips");
                    SliderControl("Întârziere afișare explicații","tooltipDelay",0,10," secunde");
                    Check("Anunță informații suplimentare despre elementul selectat","announceExtra");
                    SliderControl("Întârziere anunțare informații suplimentare","announceDelay",0,10," secunde");
                    Check("Folosește vizualizarea încorporată","embedded"); Back(); break;
                case "channels":
                    Header("Canale","Canalele de mesaje și notificări.");
                    foreach(var c in Channels) Check(c,"channel:"+c); Back("reading"); break;
                case "views":
                    Header("Vizualizări","Vizualizările disponibile.");
                    foreach(var v in Views) Check(v,"view:"+v);
                    Button("Adaugă vizualizare",AddView); Back("reading"); break;
                case "colors":
                    Header("Culori","Palete și culori personalizate.");
                    Combo("Paletă","palette",Palette);
                    Text("Culoarea textului","textColor"); Text("Culoarea fundalului","backgroundColor");
                    Text("Culoarea textului elementului selectat","selectedTextColor");
                    Text("Culoarea fundalului elementului selectat","selectedBackgroundColor");
                    Text("Culoarea textului dezactivat","disabledTextColor");
                    Check("Folosește culori predefinite pentru fundal deschis","lightColors");
                    Check("Folosește culori predefinite pentru fundal închis","darkColors"); Back("reading"); break;
                case "fonts":
                    Header("Font","Fonturile instalate în Windows sunt detectate automat.");
                    var fonts=System.Windows.Media.Fonts.SystemFontFamilies.Select(x=>x.Source).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(x=>x,StringComparer.CurrentCultureIgnoreCase).ToArray();
                    Combo("Familie de fonturi","font",fonts); Note($"Fonturi detectate: {fonts.Length}."); Back("reading"); break;
                case "security":
                    Header("Securitate","Setările acestei secțiuni vor fi completate după documentarea meniului original.");
                    Note("Nu sunt inventate setări de securitate."); Back(); break;
                case "privacy":
                    Header("Confidențialitate","Setările acestei secțiuni vor fi completate după documentarea meniului original.");
                    Note("Nu sunt inventate setări de confidențialitate."); Back(); break;
            }
        }
        finally { building=false; }
    }

    private void Header(string title,string description) { PageTitle.Text=title; PageDescription.Text=description; }
    private void Label(string text) => SettingsBody.Children.Add(new TextBlock{Text=text,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,8,0,4)});
    private void Check(string label,string key)
    {
        var c=new CheckBox{Content=label,IsChecked=Bool(key),FontSize=15,MinHeight=32,Margin=new Thickness(0,4,0,7)};
        AutomationProperties.SetName(c,label); c.Checked+=(_,_)=>Put(key,true); c.Unchecked+=(_,_)=>Put(key,false); SettingsBody.Children.Add(c);
    }
    private void Combo(string label,string key,IEnumerable<string> choices)
    {
        Label(label); var c=new ComboBox{ItemsSource=choices.ToArray(),SelectedItem=Str(key),MinHeight=36,Margin=new Thickness(0,0,0,10),IsTextSearchEnabled=true};
        if(c.SelectedItem==null && c.Items.Count>0)c.SelectedIndex=0;
        AutomationProperties.SetName(c,label); c.SelectionChanged+=(_,_)=>{if(!building && c.SelectedItem is string s)Put(key,s);}; SettingsBody.Children.Add(c);
    }
    private void LanguageCombo()
    {
        Label(LocalizationManager.Get("nativeChooseLanguage"));
        var languages = LocalizationManager.AvailableLanguages
            .Select(x => new KeyValuePair<string, string>(x.Code, x.Name)).ToList();
        var combo = new ComboBox { ItemsSource = languages, DisplayMemberPath = "Value", SelectedValuePath = "Key", SelectedValue = LocalizationManager.CurrentLanguage, MinHeight = 36, Margin = new Thickness(0,0,0,10) };
        AutomationProperties.SetName(combo,"Alege o limbă");
        combo.SelectionChanged += (_,_) =>
        {
            if (!building && combo.SelectedValue is string code)
            {
                LocalizationManager.SetLanguage(code);
                values["language"] = code;
                Save();
            }
        };
        SettingsBody.Children.Add(combo);
    }

    private void SliderControl(string label,string key,double min,double max,string suffix)
    {
        Label(label); var row=new DockPanel{Margin=new Thickness(0,0,0,12)};
        var value=new TextBlock{Width=85,Text=Num(key).ToString("0.##")+suffix,VerticalAlignment=VerticalAlignment.Center}; DockPanel.SetDock(value,Dock.Right);
        var s=new Slider{Minimum=min,Maximum=max,Value=Math.Clamp(Num(key),min,max),TickFrequency=1,MinWidth=200};
        AutomationProperties.SetName(s,label); s.ValueChanged+=(_,_)=>{value.Text=s.Value.ToString("0.##")+suffix;Put(key,s.Value);};
        row.Children.Add(value);row.Children.Add(s);SettingsBody.Children.Add(row);
    }
    private void Text(string label,string key)
    {
        Label(label); var t=new TextBox{Text=Str(key),Margin=new Thickness(0,0,0,10)};
        AutomationProperties.SetName(t,label); t.LostFocus+=(_,_)=>Put(key,t.Text.Trim()); SettingsBody.Children.Add(t);
    }
    private void Note(string text) => SettingsBody.Children.Add(new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,12),Foreground=TryFindResource("ThemeDisabledTextBrush") as Brush ?? Brushes.DimGray});
    private void Button(string label,Action action)
    {
        var b=new System.Windows.Controls.Button{Content=label,HorizontalContentAlignment=HorizontalAlignment.Left,MinHeight=40,Margin=new Thickness(0,4,0,7)};
        AutomationProperties.SetName(b,label);b.Click+=(_,_)=>action();SettingsBody.Children.Add(b);
    }
    private void Back(string page="general")=>Button(LocalizationManager.Get("nativeBack"),()=>ShowPage(page));
    private void AddView()
    {
        var d=new Window{Title="Adaugă vizualizare",Width=420,Height=190,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var p=new StackPanel{Margin=new Thickness(16)};var t=new TextBox{MinHeight=34};
        p.Children.Add(new TextBlock{Text="Numele noii vizualizări:"});p.Children.Add(t);
        var b=new System.Windows.Controls.Button{Content="Adaugă",IsDefault=true,MinHeight=36,Margin=new Thickness(0,12,0,0)};
        b.Click+=(_,_)=>{if(!string.IsNullOrWhiteSpace(t.Text))d.DialogResult=true;};p.Children.Add(b);d.Content=p;
        if(d.ShowDialog()==true){Put("customView:"+t.Text.Trim(),true);MessageBox.Show(this,"Vizualizarea a fost salvată în preferințe.","Vizualizări",MessageBoxButton.OK,MessageBoxImage.Information);}
    }
    private void Window_PreviewKeyDown(object sender,KeyEventArgs e)
    {
        if(e.Key!=Key.Escape)return;e.Handled=true;
        if(currentPage is "channels" or "views" or "colors" or "fonts")ShowPage("reading");else Close();
    }
}
