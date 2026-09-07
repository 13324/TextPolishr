using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TextPolishr.Windows;

internal sealed class KeyboardHook : IDisposable
{
    private readonly NativeMethods.LowLevelKeyboardProc _callback;
    private nint _hook;

    public KeyboardHook()
    {
        _callback = OnKeyboard;
        _hook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WhKeyboardLl,
            _callback,
            NativeMethods.GetModuleHandle(null),
            0);
        if (_hook == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not install the keyboard hook.");
        }
    }

    /// <summary>Return true to consume a key-down event.</summary>
    public Func<Keys, bool>? KeyDown { get; set; }

    private nint OnKeyboard(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && (wParam == NativeMethods.WmKeydown || wParam == NativeMethods.WmSyskeydown))
        {
            try
            {
                var input = Marshal.PtrToStructure<NativeMethods.LowLevelKeyboardInput>(lParam);
                if (KeyDown?.Invoke((Keys)input.VirtualKey) == true)
                {
                    return 1;
                }
            }
            catch
            {
                // Never allow managed exceptions to cross the native hook boundary.
            }
        }
        return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook != 0)
        {
            NativeMethods.UnhookWindowsHookEx(_hook);
            _hook = 0;
        }
        GC.KeepAlive(_callback);
    }
}
