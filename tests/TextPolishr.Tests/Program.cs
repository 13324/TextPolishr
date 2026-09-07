using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using TextPolishr.Core;
using TextPolishr.Windows;

namespace TextPolishr.Tests;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var tests = new (string Name, Action Run)[]
        {
            ("Prompt variables", TestPromptVariables),
            ("Settings migration", TestSettingsMigration),
            ("Three-entry history", TestHistoryCapacity),
            ("Shortcut parser", TestShortcutParser),
            ("Native input layout", TestNativeInputLayout),
            ("Plain LLM response", TestPlainLlmResponse),
            ("Structured LLM response", TestStructuredLlmResponse),
            ("Clipboard snapshot", TestClipboardSnapshot),
            ("Receipt-sequenced paste", TestReliablePaste),
            ("Settings UI and preset persistence", SettingsUiSmoke.Run),
            ("Recovery UI", HistoryUiSmoke.Run),
            ("Transient UI", TransientUiSmoke.Run)
        };

        var failures = new List<string>();
        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine($"PASS  {test.Name}");
            }
            catch (Exception exception)
            {
                failures.Add($"{test.Name}: {exception.Message}");
                Console.WriteLine($"FAIL  {test.Name}: {exception.Message}");
            }
        }
        return failures.Count == 0 ? 0 : 1;
    }

    private static void TestPromptVariables()
    {
        Equal("Rewrite: hello / shorter", PromptTemplate.Render("Rewrite: ${output} / ${instruction}", "hello", "shorter"));
        True(PromptTemplate.ContainsOutput("${output}"));
    }

    private static void TestSettingsMigration()
    {
        var directory = TestDirectory();
        try
        {
            var store = new SettingsStore(directory);
            var settings = store.Load();
            Equal(10_000, settings.CharacterLimit);
            True(settings.Providers.Any(provider => provider.Id == "custom"));
            True(settings.Actions.Count >= 4);
            var reloaded = store.Load();
            Equal(settings.Actions.Count, reloaded.Actions.Count);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static void TestHistoryCapacity()
    {
        var directory = TestDirectory();
        try
        {
            var history = new HistoryStore(directory);
            for (var index = 0; index < 4; index++) history.Add("Improve", $"old-{index}", $"new-{index}");
            Equal(3, history.Entries.Count);
            Equal("old-3", history.Entries[0].OriginalText);
            Equal("old-1", history.Entries[2].OriginalText);
            Equal(3, new HistoryStore(directory).Entries.Count);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static void TestShortcutParser()
    {
        True(HotkeyManager.Shortcut.TryParse("Ctrl+Alt+Space", out var shortcut));
        True(shortcut.Modifiers != 0);
        Equal((uint)Keys.Space, shortcut.VirtualKey);
        True(!HotkeyManager.Shortcut.TryParse("Ctrl+NothingHere", out _));
    }

    private static void TestNativeInputLayout()
    {
        Equal(IntPtr.Size == 8 ? 40 : 28, Marshal.SizeOf<NativeMethods.Input>());
    }

    private static void TestPlainLlmResponse()
    {
        using var client = new LlmClient(new FakeHandler("{\"choices\":[{\"message\":{\"content\":\"Revised\"}}]}"));
        var provider = new ProviderSettings { Id = "custom", Label = "Custom", BaseUrl = "http://localhost/v1" };
        var result = client.TransformAsync(provider, "", "model", "prompt", TimeSpan.FromSeconds(1), CancellationToken.None).GetAwaiter().GetResult();
        Equal("Revised", result);
    }

    private static void TestStructuredLlmResponse()
    {
        const string response = "{\"choices\":[{\"message\":{\"content\":\"{\\\"replacement_text\\\":\\\"Structured\\\"}\"}}]}";
        using var client = new LlmClient(new FakeHandler(response));
        var provider = new ProviderSettings { Id = "openai", Label = "OpenAI", BaseUrl = "https://api.openai.com/v1", SupportsStructuredOutput = true };
        var result = client.TransformAsync(provider, "key", "model", "prompt", TimeSpan.FromSeconds(1), CancellationToken.None).GetAwaiter().GetResult();
        Equal("Structured", result);
    }

    private static void TestClipboardSnapshot()
    {
        using var userClipboard = ClipboardSnapshot.Capture();
        try
        {
            ClipboardSnapshot.WriteUnicodeText("original clipboard");
            using var snapshot = ClipboardSnapshot.Capture();
            ClipboardSnapshot.WriteUnicodeText("temporary clipboard");
            snapshot.Restore();
            Equal("original clipboard", ClipboardSnapshot.ReadUnicodeText());
        }
        finally
        {
            userClipboard.Restore();
        }
    }

    private static void TestReliablePaste()
    {
        using var userClipboard = ClipboardSnapshot.Capture();
        using var form = new Form
        {
            Text = "Text Polishr integration target",
            Size = new Size(400, 120),
            StartPosition = FormStartPosition.CenterScreen,
            ShowInTaskbar = false,
            TopMost = true
        };
        var textBox = new TextBox { Text = "hello world", Dock = DockStyle.Fill };
        form.Controls.Add(textBox);
        try
        {
            _ = form.Handle;
            _ = textBox.Handle;
            textBox.Select(6, 5);
            using var paste = new ReliablePasteService(() =>
                textBox.SelectedText = ClipboardSnapshot.ReadUnicodeText() ?? string.Empty);
            var pasteTask = paste.PasteAsync("planet");
            while (!pasteTask.IsCompleted)
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }
            True(pasteTask.GetAwaiter().GetResult());
            PumpFor(TimeSpan.FromMilliseconds(100));
            Equal("hello planet", textBox.Text);
            form.Close();
        }
        finally { userClipboard.Restore(); }
    }

    private static void PumpFor(TimeSpan duration)
    {
        var until = DateTime.UtcNow + duration;
        while (DateTime.UtcNow < until)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }

    private static string TestDirectory()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "test-data-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }

    private static void True(bool value)
    {
        if (!value) throw new InvalidOperationException("Condition was false.");
    }

    private sealed class FakeHandler(string response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });
    }
}
