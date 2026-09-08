using System.Diagnostics;
using System.Runtime.InteropServices;
using TextPolishr.Core;

namespace TextPolishr.Windows;

internal sealed class SelectionService
{
    public string? LastFailure { get; private set; }

    public async Task<SelectionContext?> CaptureAsync(CancellationToken cancellationToken)
    {
        LastFailure = null;
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == 0 || !NativeMethods.IsWindow(foreground))
        {
            LastFailure = "No foreground window";
            return null;
        }

        var threadId = NativeMethods.GetWindowThreadProcessId(foreground, out var processId);
        if (processId == Environment.ProcessId)
        {
            LastFailure = "Text Polishr had foreground focus";
            return null;
        }

        var focused = GetFocusedControl(threadId);
        await WaitForShortcutReleaseAsync(cancellationToken);
        if (NativeMethods.GetForegroundWindow() != foreground || GetFocusedControl(threadId) != focused)
        {
            LastFailure = "Focus changed before copy";
            return null;
        }

        ClipboardSnapshot snapshot;
        try
        {
            snapshot = ClipboardSnapshot.Capture();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Clipboard backup failed", exception);
        }
        using (snapshot)
        {
            var beforeCopy = NativeMethods.GetClipboardSequenceNumber();
            try
            {
                NativeMethods.SendChord(NativeMethods.VkControl, NativeMethods.VkC);
            }
            catch (Exception exception)
            {
                TryRestore(snapshot);
                throw new InvalidOperationException("Ctrl+C injection failed", exception);
            }

            uint copySequence = beforeCopy;
            var deadline = Stopwatch.GetTimestamp() + (long)(Stopwatch.Frequency * 1.8);
            while (Stopwatch.GetTimestamp() < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                copySequence = NativeMethods.GetClipboardSequenceNumber();
                if (copySequence != beforeCopy)
                {
                    break;
                }
                await Task.Delay(10, cancellationToken);
            }

            if (copySequence == beforeCopy)
            {
                TryRestore(snapshot);
                LastFailure = "The target did not copy text";
                return null;
            }

            string? text;
            try
            {
                text = ClipboardSnapshot.ReadUnicodeText();
            }
            catch (Exception exception)
            {
                TryRestore(snapshot);
                throw new InvalidOperationException("Clipboard text read failed", exception);
            }
            var targetStayedActive = NativeMethods.GetForegroundWindow() == foreground;
            if (NativeMethods.GetClipboardSequenceNumber() == copySequence)
            {
                try
                {
                    snapshot.Restore();
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException("Clipboard restore failed", exception);
                }
            }

            if (!targetStayedActive)
            {
                LastFailure = "Focus changed during copy";
                return null;
            }
            if (string.IsNullOrWhiteSpace(text))
            {
                LastFailure = "The copied selection was empty";
                return null;
            }
            return new SelectionContext(foreground, focused, processId, text);
        }
    }

    public async Task<bool> ValidateAsync(SelectionContext original, CancellationToken cancellationToken)
    {
        if (NativeMethods.GetForegroundWindow() != original.ForegroundWindow)
        {
            return false;
        }

        var current = await CaptureAsync(cancellationToken);
        if (current is null || current.ForegroundWindow != original.ForegroundWindow || current.ProcessId != original.ProcessId)
        {
            return false;
        }

        if (original.FocusedControl != 0 && current.FocusedControl != 0 && current.FocusedControl != original.FocusedControl)
        {
            return false;
        }

        return NormalizeLineEndings(current.SelectedText) == NormalizeLineEndings(original.SelectedText);
    }

    public bool RestoreTarget(SelectionContext context) =>
        NativeMethods.IsWindow(context.ForegroundWindow) && NativeMethods.SetForegroundWindow(context.ForegroundWindow);

    private static nint GetFocusedControl(uint threadId)
    {
        var info = new NativeMethods.GuiThreadInfo { Size = (uint)Marshal.SizeOf<NativeMethods.GuiThreadInfo>() };
        return NativeMethods.GetGuiThreadInfo(threadId, ref info) ? info.Focus : 0;
    }

    private static async Task WaitForShortcutReleaseAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var modifiersDown =
                IsDown(NativeMethods.VkControl) ||
                IsDown(NativeMethods.VkShift) ||
                IsDown(NativeMethods.VkMenu) ||
                IsDown(NativeMethods.VkLwin) ||
                IsDown(NativeMethods.VkRwin);
            if (!modifiersDown)
            {
                return;
            }
            await Task.Delay(10, cancellationToken);
        }
    }

    private static bool IsDown(int key) => (NativeMethods.GetAsyncKeyState(key) & 0x8000) != 0;

    private static void TryRestore(ClipboardSnapshot snapshot)
    {
        try { snapshot.Restore(); }
        catch { }
    }

    private static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
