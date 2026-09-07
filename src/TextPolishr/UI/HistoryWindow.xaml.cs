using System.Windows;
using System.Windows.Input;
using TextPolishr.Core;

namespace TextPolishr.UI;

public partial class HistoryWindow : Window
{
    private readonly HistoryStore _history;

    internal HistoryWindow(HistoryStore history)
    {
        InitializeComponent();
        using (var icon = AppIcon.Create())
        {
            Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                System.Windows.Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        }
        _history = history;
        _history.Changed += History_Changed;
        RefreshEntries();
    }

    private void History_Changed(object? sender, EventArgs eventArgs)
    {
        if (!Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(RefreshEntries);
    }

    private void RefreshEntries()
    {
        HistoryItems.ItemsSource = null;
        HistoryItems.ItemsSource = _history.Entries.ToArray();
        EmptyState.Visibility = _history.Entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        HistoryScroll.Visibility = _history.Entries.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Copy_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is not System.Windows.Controls.Button { CommandParameter: HistoryEntry entry } button) return;
        System.Windows.Clipboard.SetText(button.Tag as string == "original" ? entry.OriginalText : entry.ReplacementText);
    }

    private void Clear_Click(object sender, RoutedEventArgs eventArgs) => _history.Clear();
    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs eventArgs) { if (eventArgs.ButtonState == MouseButtonState.Pressed) DragMove(); }
    private void Minimize_Click(object sender, RoutedEventArgs eventArgs) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs eventArgs) => Close();

    protected override void OnClosed(EventArgs eventArgs)
    {
        _history.Changed -= History_Changed;
        base.OnClosed(eventArgs);
    }
}
