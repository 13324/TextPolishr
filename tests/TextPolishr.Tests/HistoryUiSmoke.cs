using TextPolishr.Core;
using TextPolishr.UI;

namespace TextPolishr.Tests;

internal static class HistoryUiSmoke
{
    internal static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TextPolishr.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var history = new HistoryStore(directory);
            history.Add("Improve", "Original sentence.", "Improved sentence.");
            history.Add("Shorten", "A needlessly long sentence.", "A shorter sentence.");
            var window = new HistoryWindow(history);
            window.Show();
            window.UpdateLayout();

            var items = window.FindName("HistoryItems") as System.Windows.Controls.ItemsControl
                ?? throw new InvalidOperationException("Recovery list was not rendered.");
            if (items.Items.Count != 2) throw new InvalidOperationException("Recovery entries were not shown.");
            window.Close();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
