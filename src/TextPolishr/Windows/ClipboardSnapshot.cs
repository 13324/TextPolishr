using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TextPolishr.Windows;

/// <summary>
/// Materializes clipboard formats before Text Polishr temporarily owns the
/// clipboard. This is a C# adaptation of Handy's MIT-licensed Windows snapshot.
/// </summary>
internal sealed class ClipboardSnapshot : IDisposable
{
    private const int MaxFormatBytes = 64 * 1024 * 1024;
    private readonly List<SavedFormat> _formats;
    private nint _bitmap;
    private bool _settled;

    private ClipboardSnapshot(List<SavedFormat> formats, nint bitmap)
    {
        _formats = formats;
        _bitmap = bitmap;
    }

    public static ClipboardSnapshot Capture(nint owner = default)
    {
        OpenWithRetry(owner);
        try
        {
            var formats = new List<SavedFormat>();
            nint bitmap = 0;
            uint format = 0;
            while ((format = NativeMethods.EnumClipboardFormats(format)) != 0)
            {
                if (format == NativeMethods.CfBitmap)
                {
                    var source = NativeMethods.GetClipboardData(format);
                    if (source != 0)
                    {
                        bitmap = NativeMethods.CopyImage(
                            source,
                            NativeMethods.ImageBitmap,
                            0,
                            0,
                            NativeMethods.LrCreatedibsection);
                    }
                    continue;
                }

                if (IsNonGlobalMemoryFormat(format))
                {
                    continue;
                }

                var handle = NativeMethods.GetClipboardData(format);
                if (handle == 0)
                {
                    continue;
                }

                var size = checked((long)NativeMethods.GlobalSize(handle));
                if (size <= 0 || size > MaxFormatBytes)
                {
                    continue;
                }

                var pointer = NativeMethods.GlobalLock(handle);
                if (pointer == 0)
                {
                    continue;
                }

                try
                {
                    var bytes = new byte[size];
                    Marshal.Copy(pointer, bytes, 0, bytes.Length);
                    formats.Add(new SavedFormat(format, bytes));
                }
                finally
                {
                    NativeMethods.GlobalUnlock(handle);
                }
            }

            return new ClipboardSnapshot(formats, bitmap);
        }
        finally
        {
            NativeMethods.CloseClipboard();
        }
    }

    public void Restore()
    {
        if (_settled)
        {
            return;
        }

        OpenWithRetry(0);
        try
        {
            if (!NativeMethods.EmptyClipboard())
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not clear the clipboard for restoration.");
            }

            foreach (var saved in _formats)
            {
                var memory = Allocate(saved.Bytes);
                if (NativeMethods.SetClipboardData(saved.Format, memory) == 0)
                {
                    NativeMethods.GlobalFree(memory);
                }
            }

            if (_bitmap != 0 && NativeMethods.SetClipboardData(NativeMethods.CfBitmap, _bitmap) != 0)
            {
                _bitmap = 0; // Clipboard now owns the duplicate.
            }
        }
        finally
        {
            NativeMethods.CloseClipboard();
            _settled = true;
        }
    }

    public static string? ReadUnicodeText()
    {
        OpenWithRetry(0);
        try
        {
            var handle = NativeMethods.GetClipboardData(NativeMethods.CfUnicodeText);
            if (handle == 0)
            {
                return null;
            }

            var pointer = NativeMethods.GlobalLock(handle);
            if (pointer == 0)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUni(pointer);
            }
            finally
            {
                NativeMethods.GlobalUnlock(handle);
            }
        }
        finally
        {
            NativeMethods.CloseClipboard();
        }
    }

    public static nint AllocateUnicodeText(string text) => Allocate(
        System.Text.Encoding.Unicode.GetBytes(text + '\0'));

    internal static void WriteUnicodeText(string text)
    {
        OpenWithRetry(0);
        try
        {
            if (!NativeMethods.EmptyClipboard())
            {
                throw new InvalidOperationException("Could not clear the clipboard.");
            }
            var memory = AllocateUnicodeText(text);
            if (NativeMethods.SetClipboardData(NativeMethods.CfUnicodeText, memory) == 0)
            {
                NativeMethods.GlobalFree(memory);
                throw new InvalidOperationException("Could not write clipboard text.");
            }
        }
        finally
        {
            NativeMethods.CloseClipboard();
        }
    }

    public static nint AllocateUInt32(uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        return Allocate(bytes);
    }

    public static void OpenWithRetry(nint owner)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        do
        {
            if (NativeMethods.OpenClipboard(owner))
            {
                return;
            }
            Thread.Sleep(20);
        }
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(2));

        throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not open the Windows clipboard after 2 seconds.");
    }

    private static nint Allocate(byte[] bytes)
    {
        var memory = NativeMethods.GlobalAlloc(NativeMethods.GmemMoveable, (nuint)bytes.Length);
        if (memory == 0)
        {
            throw new OutOfMemoryException("Could not allocate clipboard memory.");
        }

        var pointer = NativeMethods.GlobalLock(memory);
        if (pointer == 0)
        {
            NativeMethods.GlobalFree(memory);
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not lock clipboard memory.");
        }

        Marshal.Copy(bytes, 0, pointer, bytes.Length);
        NativeMethods.GlobalUnlock(memory);
        return memory;
    }

    private static bool IsNonGlobalMemoryFormat(uint format) => format is
        NativeMethods.CfEnhMetafile or
        NativeMethods.CfDspEnhMetafile or
        NativeMethods.CfDspBitmap or
        NativeMethods.CfDspMetafilePict or
        NativeMethods.CfDspText or
        NativeMethods.CfOwnerDisplay or
        NativeMethods.CfPalette;

    public void Dispose()
    {
        if (_bitmap != 0)
        {
            NativeMethods.DeleteObject(_bitmap);
            _bitmap = 0;
        }
        _settled = true;
    }

    private sealed record SavedFormat(uint Format, byte[] Bytes);
}
