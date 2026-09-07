using TextPolishr.Core;
using TextPolishr.UI;
using TextPolishr.Windows;

namespace TextPolishr.App;

internal sealed class TextPolishrContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore = new();
    private readonly HistoryStore _history = new();
    private readonly LlmClient _llm = new();
    private readonly OverlayForm _overlay = new();
    private readonly HotkeyManager _hotkeys = new();
    private readonly KeyboardHook _keyboardHook = new();
    private readonly NotifyIcon _tray;
    private readonly ToolStripMenuItem _cancelItem;
    private readonly AppSettings _settings;
    private readonly TransformCoordinator _coordinator;
    private readonly System.Windows.Forms.Timer? _startupTimer;
    private readonly Control _mainThreadInvoker = new();
    private readonly object _settingsGate = new();
    private readonly ManualResetEventSlim _wpfReady = new(false);
    private SettingsWindow? _settingsWindow;
    private Thread? _wpfThread;
    private System.Windows.Application? _wpfApplication;
    private System.Windows.Threading.Dispatcher? _wpfDispatcher;
    private HistoryWindow? _historyWindow;
    private bool _disposed;

    public TextPolishrContext(bool openSettings = false)
    {
        _settings = _settingsStore.Load();
        _ = _mainThreadInvoker.Handle;
        _coordinator = new TransformCoordinator(_settings, _llm, _history, _overlay, _keyboardHook);

        var menu = new ContextMenuStrip
        {
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            Font = Theme.UiFont(9.25F),
            Renderer = new ToolStripProfessionalRenderer(new PolishrColorTable()),
            Padding = new Padding(6),
            ShowImageMargin = false
        };
        var brand = new ToolStripMenuItem("TEXT POLISHR")
        {
            Enabled = false,
            ForeColor = Theme.Accent,
            Font = Theme.UiFont(8F, FontStyle.Bold)
        };
        var transform = new ToolStripMenuItem($"Transform selection  ({_settings.MenuShortcut})", null, (_, _) => _coordinator.OpenActionMenu());
        var recent = new ToolStripMenuItem("Recent transformations…", null, (_, _) => ShowHistory());
        var settings = new ToolStripMenuItem("Settings…", null, (_, _) => ShowSettings());
        _cancelItem = new ToolStripMenuItem("Cancel current operation", null, (_, _) => _coordinator.Cancel()) { Enabled = false };
        _coordinator.BusyChanged += (_, _) => _cancelItem.Enabled = _coordinator.IsBusy;
        var exit = new ToolStripMenuItem("Exit", null, (_, _) => Exit());
        menu.Items.AddRange([brand, new ToolStripSeparator(), transform, new ToolStripSeparator(), recent, settings, _cancelItem, new ToolStripSeparator(), exit]);

        _tray = new NotifyIcon
        {
            Text = "Text Polishr",
            Icon = AppIcon.Create(),
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowSettings();

        _hotkeys.MenuRequested += (_, _) => _coordinator.OpenActionMenu();
        _hotkeys.ActionRequested += (_, action) => _coordinator.RunAction(action);
        RegisterHotkeys();

        if (openSettings)
        {
            ShowSettings();
        }
        else if (string.IsNullOrWhiteSpace(_settings.Models.GetValueOrDefault(_settings.ActiveProviderId)))
        {
            _startupTimer = new System.Windows.Forms.Timer { Interval = 150 };
            _startupTimer.Tick += (_, _) =>
            {
                _startupTimer.Stop();
                ShowSettings();
            };
            _startupTimer.Start();
        }
    }

    private void RegisterHotkeys()
    {
        var errors = _hotkeys.Register(_settings);
        if (errors.Count > 0)
        {
            _overlay.ShowStatus(errors[0], OverlayKind.Warning, 3500);
        }
    }

    private void ShowSettings()
    {
        EnsureWpfHost();
        if (!_wpfReady.Wait(TimeSpan.FromSeconds(5)) || _wpfDispatcher is null)
        {
            _overlay.ShowStatus("Could not open settings · UI host did not start", OverlayKind.Error, 3000);
            return;
        }

        _wpfDispatcher.BeginInvoke(() =>
        {
            if (_settingsWindow is null)
            {
                var window = new SettingsWindow(_settings, _settingsStore, _llm);
                window.SettingsSaved += (_, _) =>
                {
                    if (!_disposed) _mainThreadInvoker.BeginInvoke((Action)RegisterHotkeys);
                };
                window.Closed += (_, _) => _settingsWindow = null;
                _settingsWindow = window;
            }

            if (_settingsWindow.WindowState == System.Windows.WindowState.Minimized)
            {
                _settingsWindow.WindowState = System.Windows.WindowState.Normal;
            }
            _settingsWindow.Show();
            _settingsWindow.Activate();
        });
    }

    private void EnsureWpfHost()
    {
        lock (_settingsGate)
        {
            if (_wpfThread is not null) return;
            _wpfThread = new Thread(RunWpfHost)
            {
                IsBackground = true,
                Name = "TextPolishr.WpfUi"
            };
            _wpfThread.SetApartmentState(ApartmentState.STA);
            _wpfThread.Start();
        }
    }

    private void RunWpfHost()
    {
        try
        {
            _wpfApplication = new System.Windows.Application
            {
                ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
            };
            _wpfDispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            _wpfReady.Set();
            _wpfApplication.Run();
        }
        catch (Exception exception)
        {
            DiagnosticLog.Error("wpf-ui-host", exception);
            _wpfReady.Set();
        }
    }

    private void ShowHistory()
    {
        EnsureWpfHost();
        if (!_wpfReady.Wait(TimeSpan.FromSeconds(5)) || _wpfDispatcher is null)
        {
            _overlay.ShowStatus("Could not open history · UI host did not start", OverlayKind.Error, 3000);
            return;
        }
        _wpfDispatcher.BeginInvoke(() =>
        {
            if (_historyWindow is null)
            {
                _historyWindow = new HistoryWindow(_history);
                _historyWindow.Closed += (_, _) => _historyWindow = null;
            }
            if (_historyWindow.WindowState == System.Windows.WindowState.Minimized)
            {
                _historyWindow.WindowState = System.Windows.WindowState.Normal;
            }
            _historyWindow.Show();
            _historyWindow.Activate();
        });
    }

    private void Exit()
    {
        _tray.Visible = false;
        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        _tray.Visible = false;
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _wpfDispatcher?.BeginInvoke(() =>
            {
                _settingsWindow?.Close();
                _historyWindow?.Close();
                _wpfApplication?.Shutdown();
            });
            _wpfReady.Dispose();
            _mainThreadInvoker.Dispose();
            _startupTimer?.Dispose();
            _coordinator.Dispose();
            _keyboardHook.Dispose();
            _hotkeys.Dispose();
            _overlay.Dispose();
            _tray.Dispose();
            _llm.Dispose();
        }
        base.Dispose(disposing);
    }
}
