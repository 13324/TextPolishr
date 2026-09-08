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

internal sealed class CatLogo : Control
{
    public CatLogo()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Size = new Size(38, 38);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        Draw(eventArgs.Graphics, ClientRectangle);
    }

    internal static void Draw(Graphics graphics, Rectangle bounds)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var scale = Math.Min(bounds.Width, bounds.Height) / 48F;
        var x = bounds.X + (bounds.Width - 48F * scale) / 2F;
        var y = bounds.Y + (bounds.Height - 48F * scale) / 2F;
        var state = graphics.Save();
        graphics.TranslateTransform(x, y);
        graphics.ScaleTransform(scale, scale);
        using var fill = new SolidBrush(Color.FromArgb(254, 254, 251));
        using var ink = new Pen(Theme.Text, 2.6F) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var blush = new SolidBrush(Color.FromArgb(242, 181, 173));
        using var rose = new SolidBrush(Color.FromArgb(239, 155, 147));

        using var head = new GraphicsPath();
        head.AddLines([new PointF(7, 23), new PointF(8, 9), new PointF(18, 15), new PointF(29, 14), new PointF(40, 8), new PointF(40, 24)]);
        head.AddArc(7, 12, 34, 31, 0, 180);
        head.CloseFigure();
        graphics.FillPath(fill, head);
        graphics.DrawPath(ink, head);
        graphics.FillEllipse(blush, 11, 30, 7, 3);
        graphics.FillEllipse(blush, 30, 30, 7, 3);
        graphics.FillEllipse(Brushes.White, 15, 23, 4, 5);
        graphics.FillEllipse(Brushes.White, 29, 23, 4, 5);
        graphics.FillEllipse(Brushes.Black, 16, 24, 2.5F, 3.5F);
        graphics.FillEllipse(Brushes.Black, 30, 24, 2.5F, 3.5F);
        graphics.FillEllipse(rose, 22, 29, 4, 2.5F);
        graphics.DrawLine(ink, 24, 32, 24, 34);
        graphics.DrawArc(ink, 19, 31, 5, 5, 0, 90);
        graphics.DrawArc(ink, 24, 31, 5, 5, 90, 90);

        graphics.FillEllipse(fill, 32, 32, 11, 12);
        graphics.DrawEllipse(ink, 32, 32, 11, 12);
        graphics.DrawLine(ink, 35, 37, 35, 39);
        graphics.DrawLine(ink, 38, 36, 38, 39);
        graphics.DrawLine(ink, 41, 37, 41, 39);
        graphics.Restore(state);
    }
}

internal static class AppIcon
{
    internal static Icon Create()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        CatLogo.Draw(graphics, new Rectangle(1, 1, 30, 30));
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
