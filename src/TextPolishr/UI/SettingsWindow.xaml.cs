using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TextPolishr.Core;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace TextPolishr.UI;

public partial class SettingsWindow : Window
{
    private static readonly WpfBrush TransparentBrush = WpfBrushes.Transparent;
    private readonly AppSettings _settings;
    private readonly SettingsStore _store;
    private readonly LlmClient _llm;
    private readonly WpfBrush _activeNav;
    private readonly WpfBrush _inactiveNav;
    private ProviderSettings? _editingProvider;
    private TransformAction? _editingPreset;
    private bool _loadingProvider;
    private bool _loadingPreset;

    internal SettingsWindow(AppSettings settings, SettingsStore store, LlmClient llm)
    {
        InitializeComponent();
        using (var icon = AppIcon.Create())
        {
            Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                System.Windows.Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        }
        _settings = settings;
        _store = store;
        _llm = llm;
        _activeNav = (WpfBrush)FindResource("SignalSoft");
        _inactiveNav = TransparentBrush;
        LoadSettings();
        ShowPage(GeneralPage, GeneralNav, "General");
    }

    internal event EventHandler? SettingsSaved;

    private void LoadSettings()
    {
        MenuShortcutBox.Text = _settings.MenuShortcut;
        CharacterLimitBox.Text = _settings.CharacterLimit.ToString();
        TimeoutBox.Text = _settings.RequestTimeoutSeconds.ToString();

        ProviderCombo.ItemsSource = _settings.Providers;
        ProviderCombo.SelectedItem = _settings.Providers.FirstOrDefault(item => item.Id == _settings.ActiveProviderId)
            ?? _settings.Providers[0];

        PresetProviderCombo.Items.Add(new ProviderChoice(null, "Use global model"));
        foreach (var provider in _settings.Providers)
        {
            PresetProviderCombo.Items.Add(new ProviderChoice(provider.Id, provider.Label));
        }
        ReloadPresets();
        if (PresetList.Items.Count > 0) PresetList.SelectedIndex = 0;
    }

    private void ShowPage(Grid page, WpfButton nav, string title)
    {
        CommitProvider();
        CommitPreset();
        GeneralPage.Visibility = Visibility.Collapsed;
        ModelPage.Visibility = Visibility.Collapsed;
        PresetsPage.Visibility = Visibility.Collapsed;
        GeneralNav.Background = _inactiveNav;
        ModelNav.Background = _inactiveNav;
        PresetsNav.Background = _inactiveNav;
        GeneralNav.Foreground = (WpfBrush)FindResource("Muted");
        ModelNav.Foreground = (WpfBrush)FindResource("Muted");
        PresetsNav.Foreground = (WpfBrush)FindResource("Muted");
        page.Visibility = Visibility.Visible;
        nav.Background = _activeNav;
        nav.Foreground = (WpfBrush)FindResource("Ink");
        HeaderPageTitle.Text = title;
    }

    private void GeneralNav_Click(object sender, RoutedEventArgs e) => ShowPage(GeneralPage, GeneralNav, "General");
    private void ModelNav_Click(object sender, RoutedEventArgs e) => ShowPage(ModelPage, ModelNav, "Model");
    private void PresetsNav_Click(object sender, RoutedEventArgs e) => ShowPage(PresetsPage, PresetsNav, "Presets");

    private void ProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingProvider || ProviderCombo.SelectedItem is not ProviderSettings provider) return;
        _loadingProvider = true;
        try
        {
            CommitProvider();
            _editingProvider = provider;
            BaseUrlBox.Text = provider.BaseUrl;
            BaseUrlBox.IsReadOnly = !provider.AllowBaseUrlEdit;
            ApiKeyBox.Password = _settings.ApiKeys.GetValueOrDefault(provider.Id, string.Empty);
            ModelCombo.ItemsSource = null;
            ModelCombo.Text = _settings.Models.GetValueOrDefault(provider.Id, string.Empty);
        }
        finally
        {
            _loadingProvider = false;
        }
    }

    private void CommitProvider()
    {
        if (_editingProvider is null) return;
        if (_editingProvider.AllowBaseUrlEdit) _editingProvider.BaseUrl = BaseUrlBox.Text.Trim();
        _settings.ApiKeys[_editingProvider.Id] = ApiKeyBox.Password.Trim();
        _settings.Models[_editingProvider.Id] = ModelCombo.Text.Trim();
        _settings.ActiveProviderId = _editingProvider.Id;
    }

    private async void RefreshModels_Click(object sender, RoutedEventArgs e)
    {
        CommitProvider();
        if (ProviderCombo.SelectedItem is not ProviderSettings provider) return;
        RefreshModelsButton.IsEnabled = false;
        RefreshModelsButton.Content = "Loading models…";
        try
        {
            var models = await _llm.FetchModelsAsync(
                provider,
                _settings.ApiKeys.GetValueOrDefault(provider.Id, string.Empty),
                TimeSpan.FromSeconds(_settings.RequestTimeoutSeconds),
                CancellationToken.None);
            ModelCombo.ItemsSource = models;
            SaveStatus.Text = models.Count == 0 ? "Provider returned no models" : $"Found {models.Count} models";
        }
        catch (Exception exception)
        {
            SaveStatus.Text = $"Could not load models: {exception.Message}";
        }
        finally
        {
            RefreshModelsButton.IsEnabled = true;
            RefreshModelsButton.Content = "Refresh available models";
        }
    }

    private void PresetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingPreset) return;
        CommitPreset();
        _loadingPreset = true;
        try
        {
            _editingPreset = PresetList.SelectedItem as TransformAction;
            if (_editingPreset is null)
            {
                ClearPresetEditor();
                return;
            }
            PresetNameBox.Text = _editingPreset.Name;
            PresetModelBox.Text = _editingPreset.Model ?? string.Empty;
            PresetShortcutBox.Text = _editingPreset.Shortcut;
            PresetPromptBox.Text = _editingPreset.Prompt;
            PresetShowInMenuCheck.IsChecked = _editingPreset.ShowInMenu;
            PresetProviderCombo.SelectedItem = PresetProviderCombo.Items.Cast<ProviderChoice>()
                .FirstOrDefault(choice => choice.Id == _editingPreset.ProviderId) ?? PresetProviderCombo.Items[0];
            SetPresetEditorEnabled(true);
            PresetScroll.ScrollToTop();
        }
        finally
        {
            _loadingPreset = false;
        }
    }

    private void CommitPreset()
    {
        if (_editingPreset is null || _loadingPreset) return;
        _editingPreset.Name = string.IsNullOrWhiteSpace(PresetNameBox.Text) ? "Untitled preset" : PresetNameBox.Text.Trim();
        _editingPreset.ProviderId = (PresetProviderCombo.SelectedItem as ProviderChoice)?.Id;
        _editingPreset.Model = string.IsNullOrWhiteSpace(PresetModelBox.Text) ? null : PresetModelBox.Text.Trim();
        _editingPreset.Shortcut = PresetShortcutBox.Text.Trim();
        _editingPreset.Prompt = PresetPromptBox.Text;
        _editingPreset.ShowInMenu = PresetShowInMenuCheck.IsChecked == true;
        PresetList.Items.Refresh();
    }

    private void ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        CommitPreset();
        SaveStatus.Text = "Preset applied — save to keep changes";
        SaveStatus.Foreground = (WpfBrush)FindResource("Signal");
    }

    private void AddPreset_Click(object sender, RoutedEventArgs e)
    {
        CommitPreset();
        var preset = new TransformAction
        {
            Name = "New preset",
            Prompt = "<text>\r\n${output}\r\n</text>\r\n\r\nReturn only the revised text."
        };
        _settings.Actions.Add(preset);
        ReloadPresets(preset);
        PresetNameBox.Focus();
        PresetNameBox.SelectAll();
    }

    private void DeletePreset_Click(object sender, RoutedEventArgs e)
    {
        if (_editingPreset is null) return;
        var index = PresetList.SelectedIndex;
        _settings.Actions.Remove(_editingPreset);
        _editingPreset = null;
        ReloadPresets();
        if (PresetList.Items.Count > 0) PresetList.SelectedIndex = Math.Clamp(index, 0, PresetList.Items.Count - 1);
        else ClearPresetEditor();
    }

    private void ReloadPresets(TransformAction? selected = null)
    {
        PresetList.ItemsSource = null;
        PresetList.ItemsSource = _settings.Actions;
        if (selected is not null) PresetList.SelectedItem = selected;
    }

    private void ClearPresetEditor()
    {
        PresetNameBox.Clear();
        PresetModelBox.Clear();
        PresetShortcutBox.Clear();
        PresetPromptBox.Clear();
        PresetShowInMenuCheck.IsChecked = false;
        PresetProviderCombo.SelectedIndex = 0;
        SetPresetEditorEnabled(false);
    }

    private void SetPresetEditorEnabled(bool enabled)
    {
        PresetNameBox.IsEnabled = enabled;
        PresetProviderCombo.IsEnabled = enabled;
        PresetModelBox.IsEnabled = enabled;
        PresetShortcutBox.IsEnabled = enabled;
        PresetPromptBox.IsEnabled = enabled;
        PresetShowInMenuCheck.IsEnabled = enabled;
    }

    private void Save_Click(object sender, RoutedEventArgs e) => Persist(true);

    private void Persist(bool showConfirmation)
    {
        CommitPreset();
        CommitProvider();
        _settings.MenuShortcut = string.IsNullOrWhiteSpace(MenuShortcutBox.Text) ? "Ctrl+Alt+Space" : MenuShortcutBox.Text.Trim();
        if (int.TryParse(CharacterLimitBox.Text, out var characterLimit)) _settings.CharacterLimit = characterLimit;
        if (int.TryParse(TimeoutBox.Text, out var timeout)) _settings.RequestTimeoutSeconds = timeout;
        _settings.MergeDefaults();
        _store.Save(_settings);
        SettingsSaved?.Invoke(this, EventArgs.Empty);
        if (showConfirmation)
        {
            SaveStatus.Text = "✓  Settings saved";
            SaveStatus.Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(74, 222, 128));
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        Persist(false);
        base.OnClosing(e);
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private sealed record ProviderChoice(string? Id, string Label)
    {
        public override string ToString() => Label;
    }
}
