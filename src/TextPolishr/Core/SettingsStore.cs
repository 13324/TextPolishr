using System.Text.Json;

namespace TextPolishr.Core;

internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsPath;

    public SettingsStore(string? appDataDirectory = null)
    {
        var directory = appDataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TextPolishr");
        Directory.CreateDirectory(directory);
        _settingsPath = Path.Combine(directory, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaults = AppSettings.CreateDefault();
            Save(defaults);
            return defaults;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath), JsonOptions)
                ?? AppSettings.CreateDefault();
            settings.MergeDefaults();
            Save(settings);
            return settings;
        }
        catch (JsonException)
        {
            var defaults = AppSettings.CreateDefault();
            var backup = _settingsPath + ".invalid-" + DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(_settingsPath, backup, overwrite: true);
            Save(defaults);
            return defaults;
        }
    }

    public void Save(AppSettings settings)
    {
        var tempPath = _settingsPath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(tempPath, _settingsPath, overwrite: true);
    }
}
