using System.Text.Json;

namespace TextPolishr.Core;

internal sealed class HistoryStore
{
    private const int Capacity = 3;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _historyPath;
    private readonly List<HistoryEntry> _entries = [];

    public HistoryStore(string? appDataDirectory = null)
    {
        var directory = appDataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TextPolishr");
        Directory.CreateDirectory(directory);
        _historyPath = Path.Combine(directory, "history.json");
        Load();
    }

    public IReadOnlyList<HistoryEntry> Entries => _entries;

    public event EventHandler? Changed;

    public void Add(string actionName, string originalText, string replacementText)
    {
        _entries.Insert(0, new HistoryEntry(Guid.NewGuid(), DateTimeOffset.Now, actionName, originalText, replacementText));
        if (_entries.Count > Capacity)
        {
            _entries.RemoveRange(Capacity, _entries.Count - Capacity);
        }
        Save();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _entries.Clear();
        Save();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Load()
    {
        if (!File.Exists(_historyPath))
        {
            return;
        }

        try
        {
            var entries = JsonSerializer.Deserialize<List<HistoryEntry>>(File.ReadAllText(_historyPath), JsonOptions) ?? [];
            _entries.AddRange(entries.OrderByDescending(entry => entry.Timestamp).Take(Capacity));
        }
        catch (JsonException)
        {
            // A damaged optional recovery buffer must never prevent startup.
        }
    }

    private void Save()
    {
        var tempPath = _historyPath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(_entries, JsonOptions));
        File.Move(tempPath, _historyPath, overwrite: true);
    }
}
