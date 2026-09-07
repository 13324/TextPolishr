using System.Runtime.InteropServices;

namespace TextPolishr.Windows;

internal static class NativeMethods
{
    internal const uint CfText = 1;
    internal const uint CfBitmap = 2;
    internal const uint CfMetafilePict = 3;
    internal const uint CfSylk = 4;
    internal const uint CfDif = 5;
    internal const uint CfTiff = 6;
    internal const uint CfOemText = 7;
    internal const uint CfDib = 8;
    internal const uint CfPalette = 9;
    internal const uint CfPendata = 10;
    internal const uint CfRiff = 11;
    internal const uint CfWave = 12;
    internal const uint CfUnicodeText = 13;
    internal const uint CfEnhMetafile = 14;
    internal const uint CfHdrop = 15;
    internal const uint CfLocale = 16;
    internal const uint CfDibV5 = 17;
    internal const uint CfOwnerDisplay = 0x0080;
    internal const uint CfDspText = 0x0081;
    internal const uint CfDspBitmap = 0x0082;
    internal const uint CfDspMetafilePict = 0x0083;
    internal const uint CfDspEnhMetafile = 0x008E;

    internal const uint GmemMoveable = 0x0002;
    internal const uint ImageBitmap = 0;
    internal const uint LrCreatedibsection = 0x00002000;
    internal const uint InputKeyboard = 1;
    internal const uint KeyeventfKeyup = 0x0002;
    internal const ushort VkControl = 0x11;
    internal const ushort VkShift = 0x10;
    internal const ushort VkMenu = 0x12;
    internal const ushort VkLwin = 0x5B;
    internal const ushort VkRwin = 0x5C;
    internal const ushort VkC = 0x43;
    internal const ushort VkV = 0x56;

    internal const int WmHotkey = 0x0312;
    internal const int WmRenderFormat = 0x0305;
    internal const int WmRenderAllFormats = 0x0306;
    internal const int WmDestroyClipboard = 0x0307;
    internal const int WmKeydown = 0x0100;
    internal const int WmSyskeydown = 0x0104;
    internal const int WhKeyboardLl = 13;

    internal const uint ModAlt = 0x0001;
    internal const uint ModControl = 0x0002;
    internal const uint ModShift = 0x0004;
    internal const uint ModWin = 0x0008;
    internal const uint ModNoRepeat = 0x4000;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GuiThreadInfo
    {
        internal uint Size;
        internal uint Flags;
        internal nint Active;
        internal nint Focus;
        internal nint Capture;
        internal nint MenuOwner;
        internal nint MoveSize;
        internal nint Caret;
        internal Rect CaretRect;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Input
    {
        internal uint Type;
        internal InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)] internal KeyboardInput Keyboard;
        [FieldOffset(0)] internal MouseInput Mouse;
        [FieldOffset(0)] internal HardwareInput Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyboardInput
    {
        internal ushort VirtualKey;
        internal ushort ScanCode;
        internal uint Flags;
        internal uint Time;
        internal nuint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MouseInput
    {
        internal int X;
        internal int Y;
        internal uint MouseData;
        internal uint Flags;
        internal uint Time;
        internal nuint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HardwareInput
    {
        internal uint Message;
        internal ushort ParameterLow;
        internal ushort ParameterHigh;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LowLevelKeyboardInput
    {
        internal uint VirtualKey;
        internal uint ScanCode;
        internal uint Flags;
        internal uint Time;
        internal nuint ExtraInfo;
    }

    internal delegate nint LowLevelKeyboardProc(int code, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool OpenClipboard(nint newOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint EnumClipboardFormats(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint GetClipboardData(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint SetClipboardData(uint format, nint memory);

    [DllImport("user32.dll")]
    internal static extern uint GetClipboardSequenceNumber();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern uint RegisterClipboardFormat(string format);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern nint GlobalAlloc(uint flags, nuint bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern nint GlobalFree(nint memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern nint GlobalLock(nint memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalUnlock(nint memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern nuint GlobalSize(nint memory);

    [DllImport("kernel32.dll")]
    internal static extern void SetLastError(uint errorCode);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint CopyImage(nint handle, uint type, int width, int height, uint flags);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteObject(nint graphicsObject);

    [DllImport("user32.dll")]
    internal static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("user32.dll", EntryPoint = "GetGUIThreadInfo", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetGuiThreadInfo(uint threadId, ref GuiThreadInfo info);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindow(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    internal static extern short GetAsyncKeyState(int virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint count, Input[] inputs, int size);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(nint window, int id);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint SetWindowsHookEx(int hookId, LowLevelKeyboardProc callback, nint module, uint threadId);

    [DllImport("user32.dll")]
    internal static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWindowsHookEx(nint hook);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint GetModuleHandle(string? moduleName);

    internal static void SendChord(params ushort[] keys)
    {
        var downs = keys.Select(KeyDown).ToArray();
        var ups = keys.Reverse().Select(KeyUp).ToArray();
        if (SendInput((uint)downs.Length, downs, Marshal.SizeOf<Input>()) != downs.Length)
        {
            _ = SendInput((uint)ups.Length, ups, Marshal.SizeOf<Input>());
            throw new InvalidOperationException("Windows did not accept the keyboard input.");
        }
        Thread.Sleep(100);
        if (SendInput((uint)ups.Length, ups, Marshal.SizeOf<Input>()) != ups.Length)
        {
            throw new InvalidOperationException("Windows did not release the keyboard shortcut.");
        }
    }

    private static Input KeyDown(ushort key) => new()
    {
        Type = InputKeyboard,
        Data = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = key } }
    };

    private static Input KeyUp(ushort key) => new()
    {
        Type = InputKeyboard,
        Data = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = key, Flags = KeyeventfKeyup } }
    };
}
