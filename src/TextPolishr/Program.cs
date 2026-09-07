using TextPolishr.App;
using TextPolishr.Core;

namespace TextPolishr;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var singleInstance = new Mutex(initiallyOwned: true, "Local\\TextPolishr.SingleInstance", out var created);
        if (!created)
        {
            MessageBox.Show("Text Polishr is already running.", "Text Polishr", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, eventArgs) => DiagnosticLog.Error("ui-thread", eventArgs.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception exception) DiagnosticLog.Error("unhandled", exception);
        };
        using var context = new TextPolishrContext(args.Contains("--settings", StringComparer.OrdinalIgnoreCase));
        Application.Run(context);
    }
}
