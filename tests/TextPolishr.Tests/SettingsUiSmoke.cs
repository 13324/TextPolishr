using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TextPolishr.Core;
using TextPolishr.UI;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfPasswordBox = System.Windows.Controls.PasswordBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace TextPolishr.Tests;

internal static class SettingsUiSmoke
{
    internal static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TextPolishr.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var store = new SettingsStore(directory);
            var settings = AppSettings.CreateDefault();
            store.Save(settings);
            using var llm = new LlmClient();
            var window = new SettingsWindow(settings, store, llm);
            window.Show();
            window.UpdateLayout();

            var presetsNav = Find<WpfButton>(window, "PresetsNav");
            presetsNav.RaiseEvent(new RoutedEventArgs(WpfButton.ClickEvent));
            var list = Find<WpfListBox>(window, "PresetList");
            Assert(list.Items.Count >= 4, "Default presets were not rendered.");
            list.SelectedIndex = 0;

            var name = Find<WpfTextBox>(window, "PresetNameBox");
            var prompt = Find<WpfTextBox>(window, "PresetPromptBox");
            Assert(name.IsEnabled && prompt.IsEnabled, "Preset editor fields are disabled.");
            name.Text = "Editorial polish";
            prompt.Text = "Revise this text:\n${output}";
            list.SelectedIndex = 1;
            Assert(settings.Actions[0].Name == "Editorial polish", "Preset edits were lost when switching presets.");

            var modelNav = Find<WpfButton>(window, "ModelNav");
            modelNav.RaiseEvent(new RoutedEventArgs(WpfButton.ClickEvent));
            var apiKey = Find<WpfPasswordBox>(window, "ApiKeyBox");
            var model = window.FindName("ModelCombo") as WpfComboBox
                ?? throw new InvalidOperationException("Model selector was not rendered.");
            apiKey.Password = "test-key";
            model.Text = "test-model";
            Find<WpfButton>(window, "SaveButton").RaiseEvent(new RoutedEventArgs(WpfButton.ClickEvent));

            var reloaded = store.Load();
            Assert(reloaded.Actions[0].Name == "Editorial polish", "Preset name was not persisted.");
            Assert(reloaded.Actions[0].Prompt.Contains("${output}", StringComparison.Ordinal), "Preset prompt was not persisted.");
            Assert(reloaded.Models[reloaded.ActiveProviderId] == "test-model", "Model selection was not persisted.");
            Assert(reloaded.ApiKeys[reloaded.ActiveProviderId] == "test-key", "API key was not persisted.");

            presetsNav.RaiseEvent(new RoutedEventArgs(WpfButton.ClickEvent));
            window.UpdateLayout();
            var snapshotPath = Environment.GetEnvironmentVariable("TEXTPOLISHR_UI_SNAPSHOT");
            if (!string.IsNullOrWhiteSpace(snapshotPath)) Render(window, snapshotPath);
            window.Close();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static T Find<T>(FrameworkElement root, string name) where T : FrameworkElement =>
        root.FindName(name) as T ?? throw new InvalidOperationException($"UI element '{name}' was not found.");

    private static void Render(Window window, string path)
    {
        var width = Math.Max(1, (int)Math.Ceiling(window.ActualWidth));
        var height = Math.Max(1, (int)Math.Ceiling(window.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
