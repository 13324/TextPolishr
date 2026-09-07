using System.ComponentModel;
using System.Runtime.InteropServices;
using TextPolishr.Core;

namespace TextPolishr.Windows;

internal sealed class HotkeyManager : NativeWindow, IDisposable
{
    private const int MenuId = 100;
    private readonly Dictionary<int, TransformAction> _actions = [];

    public HotkeyManager()
    {
        CreateHandle(new CreateParams { Caption = "TextPolishrHotkeys", Parent = new nint(-3) });
    }

    public event EventHandler? MenuRequested;
    public event EventHandler<TransformAction>? ActionRequested;

    public IReadOnlyList<string> Register(AppSettings settings)
    {
        UnregisterAll();
        var errors = new List<string>();
        if (!RegisterOne(MenuId, settings.MenuShortcut))
        {
            errors.Add($"Could not register {settings.MenuShortcut} for the action menu.");
        }

        var id = 1000;
        foreach (var action in settings.Actions.Where(action => !string.IsNullOrWhiteSpace(action.Shortcut)))
        {
            if (RegisterOne(id, action.Shortcut))
            {
                _actions[id] = action;
            }
            else
            {
                errors.Add($"Could not register {action.Shortcut} for {action.Name}.");
            }
            id++;
        }
        return errors;
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WmHotkey)
        {
            var id = message.WParam.ToInt32();
            if (id == MenuId)
            {
                MenuRequested?.Invoke(this, EventArgs.Empty);
            }
            else if (_actions.TryGetValue(id, out var action))
            {
                ActionRequested?.Invoke(this, action);
            }
            return;
        }
        base.WndProc(ref message);
    }

    private bool RegisterOne(int id, string shortcut)
    {
        return Shortcut.TryParse(shortcut, out var parsed) &&
            NativeMethods.RegisterHotKey(Handle, id, parsed.Modifiers | NativeMethods.ModNoRepeat, parsed.VirtualKey);
    }

    private void UnregisterAll()
    {
        NativeMethods.UnregisterHotKey(Handle, MenuId);
        foreach (var id in _actions.Keys)
        {
            NativeMethods.UnregisterHotKey(Handle, id);
        }
        _actions.Clear();
    }

    public void Dispose()
    {
        UnregisterAll();
        DestroyHandle();
    }

    internal readonly record struct Shortcut(uint Modifiers, uint VirtualKey)
    {
        internal static bool TryParse(string text, out Shortcut shortcut)
        {
            shortcut = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            uint modifiers = 0;
            uint key = 0;
            foreach (var rawPart in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                switch (rawPart.ToLowerInvariant())
                {
                    case "ctrl":
                    case "control": modifiers |= NativeMethods.ModControl; break;
                    case "alt": modifiers |= NativeMethods.ModAlt; break;
                    case "shift": modifiers |= NativeMethods.ModShift; break;
                    case "win":
                    case "windows": modifiers |= NativeMethods.ModWin; break;
                    case "space": key = (uint)Keys.Space; break;
                    case "escape":
                    case "esc": key = (uint)Keys.Escape; break;
                    default:
                        if (Enum.TryParse<Keys>(rawPart, ignoreCase: true, out var parsedKey))
                        {
                            key = (uint)parsedKey;
                        }
                        else if (rawPart.Length == 1)
                        {
                            key = char.ToUpperInvariant(rawPart[0]);
                        }
                        else
                        {
                            return false;
                        }
                        break;
                }
            }

            if (key == 0)
            {
                return false;
            }
            shortcut = new Shortcut(modifiers, key);
            return true;
        }
    }
}
