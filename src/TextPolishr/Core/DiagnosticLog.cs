namespace TextPolishr.Core;

internal static class DiagnosticLog
{
    private static readonly object Sync = new();
    private static readonly string PathName = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TextPolishr",
        "diagnostics.log");

    internal static void Error(string operation, Exception exception)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);
                File.AppendAllText(
                    PathName,
                    $"{DateTimeOffset.Now:O} ERROR {operation}: {Describe(exception)}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostics must never affect the transformation workflow.
        }
    }

    private static string Describe(Exception exception)
    {
        var parts = new List<string>();
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            parts.Add($"{current.GetType().Name}: {Sanitize(current.Message)}");
        }
        return string.Join(" -> ", parts);
    }

    private static string Sanitize(string message)
    {
        var singleLine = message.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return singleLine.Length <= 500 ? singleLine : singleLine[..500];
    }
}
