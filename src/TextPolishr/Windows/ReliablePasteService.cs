using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TextPolishr.Windows;

/// <summary>
/// Receipt-sequenced paste based on Handy's MIT-licensed Windows design.
/// The target's WM_RENDERFORMAT request is the receipt proving that it read
/// the replacement before the old clipboard is restored.
/// </summary>
internal sealed class ReliablePasteService : NativeWindow, IDisposable
{
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(125);
    private static readonly TimeSpan RestoreTimeout = TimeSpan.FromSeconds(8);
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Action _sendPasteChord;
    private PasteState? _state;

    public ReliablePasteService(Action? sendPasteChord = null)
    {
        CreateHandle(new CreateParams
        {
            Caption = "TextPolishrClipboardOwner",
            Parent = new nint(-3) // HWND_MESSAGE
        });
        _timer = new System.Windows.Forms.Timer { Interval = 15 };
        _timer.Tick += OnTimer;
        _sendPasteChord = sendPasteChord ?? (() => NativeMethods.SendChord(NativeMethods.VkControl, NativeMethods.VkV));
    }

    public Task<bool> PasteAsync(string text)
    {
        if (_state is not null)
        {
            throw new InvalidOperationException("A paste transaction is already active.");
        }

        var snapshot = ClipboardSnapshot.Capture(Handle);
        try
        {
            ClipboardSnapshot.OpenWithRetry(Handle);
            try
            {
                if (!NativeMethods.EmptyClipboard())
                {
                    throw new InvalidOperationException("Could not publish replacement text.");
                }

                PublishMarker("ExcludeClipboardContentFromMonitorProcessing", 1);
                PublishMarker("CanIncludeInClipboardHistory", 0);
                PublishMarker("CanUploadToCloudClipboard", 0);

                NativeMethods.SetLastError(0);
                _ = NativeMethods.SetClipboardData(NativeMethods.CfUnicodeText, 0);
                if (Marshal.GetLastWin32Error() != 0)
                {
                    throw new InvalidOperationException("Could not register delayed clipboard rendering.");
                }
            }
            finally
            {
                NativeMethods.CloseClipboard();
            }

            var state = new PasteState(text, snapshot, NativeMethods.GetClipboardSequenceNumber());
            _state = state;
            state.InjectedAt = Stopwatch.GetTimestamp();
            try
            {
                _sendPasteChord();
            }
            catch
            {
                state.InjectionFailed = true;
            }
            _timer.Start();
            return state.Completion.Task;
        }
        catch
        {
            snapshot.Restore();
            snapshot.Dispose();
            throw;
        }
    }

    protected override void WndProc(ref Message message)
    {
        switch (message.Msg)
        {
            case NativeMethods.WmRenderFormat:
                RenderText(recordReceipt: true);
                message.Result = 0;
                return;
            case NativeMethods.WmRenderAllFormats:
                if (_state is not null)
                {
                    ClipboardSnapshot.OpenWithRetry(Handle);
                    try { RenderText(recordReceipt: false); }
                    finally { NativeMethods.CloseClipboard(); }
                }
                message.Result = 0;
                return;
            case NativeMethods.WmDestroyClipboard:
                if (_state is { Settling: false } state)
                {
                    state.OwnershipLost = true;
                }
                break;
        }
        base.WndProc(ref message);
    }

    private void RenderText(bool recordReceipt)
    {
        var state = _state;
        if (state is null)
        {
            return;
        }

        var memory = ClipboardSnapshot.AllocateUnicodeText(state.Text);
        if (NativeMethods.SetClipboardData(NativeMethods.CfUnicodeText, memory) == 0)
        {
            NativeMethods.GlobalFree(memory);
            return;
        }

        if (recordReceipt)
        {
            state.LastReceiptAt = Stopwatch.GetTimestamp();
            state.ReceivedAfterInjection = state.LastReceiptAt >= state.InjectedAt;
        }
    }

    private void OnTimer(object? sender, EventArgs eventArgs)
    {
        var state = _state;
        if (state is null)
        {
            _timer.Stop();
            return;
        }

        var now = Stopwatch.GetTimestamp();
        var sincePublished = Stopwatch.GetElapsedTime(state.PublishedAt, now);
        var quiet = state.LastReceiptAt is { } receipt && Stopwatch.GetElapsedTime(receipt, now) >= QuietPeriod;
        var finish = state.OwnershipLost || quiet || sincePublished >= RestoreTimeout ||
            (state.InjectionFailed && sincePublished >= TimeSpan.FromMilliseconds(500));
        if (!finish)
        {
            return;
        }

        _timer.Stop();
        state.Settling = true;
        try
        {
            if (!state.OwnershipLost && NativeMethods.GetClipboardSequenceNumber() == state.Sequence)
            {
                state.Snapshot.Restore();
            }
        }
        finally
        {
            state.Snapshot.Dispose();
            _state = null;
            state.Completion.TrySetResult(state.ReceivedAfterInjection && !state.InjectionFailed);
        }
    }

    private static void PublishMarker(string name, uint value)
    {
        var format = NativeMethods.RegisterClipboardFormat(name);
        if (format == 0)
        {
            return;
        }
        var memory = ClipboardSnapshot.AllocateUInt32(value);
        if (NativeMethods.SetClipboardData(format, memory) == 0)
        {
            NativeMethods.GlobalFree(memory);
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        if (_state is { } state)
        {
            state.Snapshot.Dispose();
            state.Completion.TrySetCanceled();
            _state = null;
        }
        DestroyHandle();
    }

    private sealed class PasteState(string text, ClipboardSnapshot snapshot, uint sequence)
    {
        internal string Text { get; } = text;
        internal ClipboardSnapshot Snapshot { get; } = snapshot;
        internal uint Sequence { get; } = sequence;
        internal long PublishedAt { get; } = Stopwatch.GetTimestamp();
        internal long InjectedAt { get; set; }
        internal long? LastReceiptAt { get; set; }
        internal bool ReceivedAfterInjection { get; set; }
        internal bool OwnershipLost { get; set; }
        internal bool InjectionFailed { get; set; }
        internal bool Settling { get; set; }
        internal TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
