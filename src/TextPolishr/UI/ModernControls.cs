using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace TextPolishr.UI;

internal sealed class RoundedPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 14;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Theme.BorderSoft;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int BorderWidth { get; set; } = 1;

    public RoundedPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Surface;
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Geometry.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
        using var background = new SolidBrush(BackColor);
        eventArgs.Graphics.FillPath(background, path);
        if (BorderWidth > 0)
        {
            using var pen = new Pen(BorderColor, BorderWidth);
            eventArgs.Graphics.DrawPath(pen, path);
        }
    }

    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        using var path = Geometry.RoundRect(new Rectangle(0, 0, Width, Height), Radius);
        Region = new Region(path);
    }
}

internal sealed class PillLabel : Label
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color PillColor { get; set; } = Theme.AccentSoft;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 8;

    public PillLabel()
    {
        AutoSize = false;
        TextAlign = ContentAlignment.MiddleCenter;
        ForeColor = Theme.AccentHover;
        Font = Theme.UiFont(8F, FontStyle.Bold);
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Geometry.RoundRect(ClientRectangle, Radius);
        using var brush = new SolidBrush(PillColor);
        eventArgs.Graphics.FillPath(brush, path);
        TextRenderer.DrawText(eventArgs.Graphics, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

internal sealed class ModernTabControl : TabControl
{
    public ModernTabControl()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Fixed;
        ItemSize = new Size(132, 44);
        Padding = new Point(16, 7);
        BackColor = Theme.Background;
        Font = Theme.UiFont(9.5F, FontStyle.Bold);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnDrawItem(DrawItemEventArgs eventArgs)
    {
        var selected = eventArgs.Index == SelectedIndex;
        var bounds = GetTabRect(eventArgs.Index);
        using var background = new SolidBrush(Theme.Background);
        eventArgs.Graphics.FillRectangle(background, bounds);
        TextRenderer.DrawText(eventArgs.Graphics, TabPages[eventArgs.Index].Text, Font, bounds,
            selected ? Theme.Text : Theme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        if (selected)
        {
            using var accent = new SolidBrush(Theme.Accent);
            eventArgs.Graphics.FillRectangle(accent, bounds.Left + 18, bounds.Bottom - 3, bounds.Width - 36, 3);
        }
    }
}

internal sealed class ModernListBox : ListBox
{
    public ModernListBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        ItemHeight = 44;
        BorderStyle = BorderStyle.None;
        BackColor = Theme.SurfaceRaised;
        ForeColor = Theme.Text;
        Font = Theme.UiFont(9.5F, FontStyle.Bold);
        IntegralHeight = false;
    }

    protected override void OnDrawItem(DrawItemEventArgs eventArgs)
    {
        if (eventArgs.Index < 0) return;
        var selected = (eventArgs.State & DrawItemState.Selected) != 0;
        using var background = new SolidBrush(selected ? Theme.SurfaceHover : Theme.SurfaceRaised);
        eventArgs.Graphics.FillRectangle(background, eventArgs.Bounds);
        if (selected)
        {
            using var accent = new SolidBrush(Theme.Accent);
            eventArgs.Graphics.FillRectangle(accent, eventArgs.Bounds.Left, eventArgs.Bounds.Top + 7, 3, eventArgs.Bounds.Height - 14);
        }
        var textBounds = new Rectangle(eventArgs.Bounds.Left + 16, eventArgs.Bounds.Top,
            eventArgs.Bounds.Width - 24, eventArgs.Bounds.Height);
        TextRenderer.DrawText(eventArgs.Graphics, GetItemText(Items[eventArgs.Index]), Font, textBounds,
            selected ? Theme.Text : Theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }
}

internal sealed class ActionRow : Control
{
    private bool _hovered;
    private bool _selected;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string KeyText { get; init; } = string.Empty;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string LabelText { get; init; } = string.Empty;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Muted { get; init; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsSelected
    {
        get => _selected;
        set { _selected = value; Invalidate(); }
    }

    public ActionRow()
    {
        Height = 48;
        Width = 324;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public void PerformClick() => OnClick(EventArgs.Empty);

    protected override void OnMouseEnter(EventArgs eventArgs)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(eventArgs);
    }

    protected override void OnMouseLeave(EventArgs eventArgs)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(eventArgs);
    }

    protected override void OnMouseUp(MouseEventArgs eventArgs)
    {
        if (eventArgs.Button == MouseButtons.Left && ClientRectangle.Contains(eventArgs.Location))
        {
            OnClick(EventArgs.Empty);
        }
        base.OnMouseUp(eventArgs);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var active = _selected || _hovered;
        if (active)
        {
            using var path = Geometry.RoundRect(new Rectangle(0, 1, Width - 1, Height - 2), 10);
            using var brush = new SolidBrush(Theme.SurfaceHover);
            eventArgs.Graphics.FillPath(brush, path);
        }

        var keyBounds = new Rectangle(12, 12, 27, 24);
        using (var keyPath = Geometry.RoundRect(keyBounds, 6))
        using (var keyBrush = new SolidBrush(active && !Muted ? Theme.AccentSoft : Theme.SurfaceRaised))
        using (var keyPen = new Pen(active && !Muted ? Theme.Accent : Theme.BorderSoft))
        {
            eventArgs.Graphics.FillPath(keyBrush, keyPath);
            eventArgs.Graphics.DrawPath(keyPen, keyPath);
        }
        TextRenderer.DrawText(eventArgs.Graphics, KeyText, Theme.UiFont(8F, FontStyle.Bold), keyBounds,
            active && !Muted ? Theme.AccentHover : Theme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        var textBounds = new Rectangle(53, 0, Width - 88, Height);
        TextRenderer.DrawText(eventArgs.Graphics, LabelText, Theme.UiFont(10F, FontStyle.Bold), textBounds,
            Muted ? Theme.Muted : Theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        if (active && !Muted)
        {
            var arrowBounds = new Rectangle(Width - 35, 0, 20, Height);
            TextRenderer.DrawText(eventArgs.Graphics, "→", Theme.UiFont(11F), arrowBounds, Theme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}

internal sealed class PolishrColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => Theme.Surface;
    public override Color ImageMarginGradientBegin => Theme.Surface;
    public override Color ImageMarginGradientMiddle => Theme.Surface;
    public override Color ImageMarginGradientEnd => Theme.Surface;
    public override Color MenuBorder => Theme.Border;
    public override Color MenuItemBorder => Theme.SurfaceHover;
    public override Color MenuItemSelected => Theme.SurfaceHover;
    public override Color MenuItemSelectedGradientBegin => Theme.SurfaceHover;
    public override Color MenuItemSelectedGradientEnd => Theme.SurfaceHover;
    public override Color MenuItemPressedGradientBegin => Theme.SurfaceRaised;
    public override Color MenuItemPressedGradientEnd => Theme.SurfaceRaised;
    public override Color SeparatorDark => Theme.BorderSoft;
    public override Color SeparatorLight => Theme.BorderSoft;
}

internal static class AppIcon
{
    internal static Icon Create()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var background = new SolidBrush(Theme.Accent);
        graphics.FillPath(background, Geometry.RoundRect(new Rectangle(2, 2, 28, 28), 8));
        using var mark = new Pen(Color.White, 2.6F) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        graphics.DrawLine(mark, 10, 10, 22, 10);
        graphics.DrawLine(mark, 16, 10, 16, 22);
        graphics.DrawLine(mark, 11, 22, 21, 22);
        var handle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            _ = DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(nint icon);
}

internal static class Geometry
{
    internal static GraphicsPath RoundRect(Rectangle rectangle, int radius)
    {
        var path = new GraphicsPath();
        if (rectangle.Width <= 0 || rectangle.Height <= 0) return path;
        var diameter = Math.Min(radius * 2, Math.Min(rectangle.Width, rectangle.Height));
        if (diameter <= 0)
        {
            path.AddRectangle(rectangle);
            return path;
        }
        var arc = new Rectangle(rectangle.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = rectangle.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rectangle.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rectangle.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
