using System.Drawing.Drawing2D;
using TextPolishr.Core;

namespace TextPolishr.UI;

internal sealed class OverlayForm : Form
{
    private readonly Label _title;
    private readonly Label _detail;
    private readonly Panel _signal;
    private readonly System.Windows.Forms.Timer _hideTimer;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Theme.Surface;
        ClientSize = new Size(408, 66);
        Padding = new Padding(0);

        _signal = new Panel
        {
            BackColor = Theme.Accent,
            Location = new Point(0, 0),
            Size = new Size(4, 66)
        };
        _title = new Label
        {
            AutoEllipsis = true,
            Location = new Point(24, 12),
            Size = new Size(362, 23),
            Font = Theme.UiFont(10.25F, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Color.Transparent
        };
        _detail = new Label
        {
            AutoEllipsis = true,
            Location = new Point(24, 36),
            Size = new Size(362, 18),
            Font = Theme.UiFont(8.75F),
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent
        };
        Controls.AddRange([_signal, _title, _detail]);
        _hideTimer = new System.Windows.Forms.Timer();
        _hideTimer.Tick += (_, _) => { _hideTimer.Stop(); Hide(); };
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int wsExNoActivate = 0x08000000;
            const int wsExToolWindow = 0x00000080;
            const int csDropShadow = 0x00020000;
            var parameters = base.CreateParams;
            parameters.ExStyle |= wsExNoActivate | wsExToolWindow;
            parameters.ClassStyle |= csDropShadow;
            return parameters;
        }
    }

    public void ShowStatus(string text, OverlayKind kind, int? hideAfterMilliseconds = null)
    {
        _hideTimer.Stop();
        var parts = text.Replace("Â·", "·", StringComparison.Ordinal).Split('·', 2,
            StringSplitOptions.TrimEntries);
        _title.Text = parts[0];
        _detail.Text = parts.Length > 1 ? parts[1] : kind == OverlayKind.Progress ? "Working on your selection" : "Text Polishr";
        _signal.BackColor = Theme.StatusColor(kind);

        var area = Screen.GetWorkingArea(Cursor.Position);
        Location = new Point(area.Left + (area.Width - Width) / 2, area.Bottom - Height - 24);
        Show();
        if (hideAfterMilliseconds is { } delay)
        {
            _hideTimer.Interval = delay;
            _hideTimer.Start();
        }
    }

    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        using var path = Geometry.RoundRect(ClientRectangle, 15);
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var border = new Pen(Theme.Border);
        using var path = Geometry.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 15);
        eventArgs.Graphics.DrawPath(border, path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hideTimer.Dispose();
        base.Dispose(disposing);
    }
}
