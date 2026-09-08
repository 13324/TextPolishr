using System.Runtime.InteropServices;

namespace TextPolishr.UI;

internal static class Theme
{
    // Paper + oxide. High-contrast, utilitarian and deliberately free of gradients.
    internal static readonly Color Background = Color.FromArgb(247, 248, 245);
    internal static readonly Color Sidebar = Color.White;
    internal static readonly Color Surface = Color.White;
    internal static readonly Color SurfaceRaised = Color.FromArgb(244, 246, 243);
    internal static readonly Color SurfaceHover = Color.FromArgb(233, 237, 232);
    internal static readonly Color Border = Color.FromArgb(210, 218, 211);
    internal static readonly Color BorderSoft = Color.FromArgb(226, 231, 226);
    internal static readonly Color Text = Color.FromArgb(23, 33, 43);
    internal static readonly Color Muted = Color.FromArgb(67, 82, 96);
    internal static readonly Color Subtle = Color.FromArgb(100, 116, 130);
    internal static readonly Color Accent = Color.FromArgb(182, 64, 38);
    internal static readonly Color AccentHover = Color.FromArgb(151, 48, 28);
    internal static readonly Color AccentSoft = Color.FromArgb(248, 231, 225);
    internal static readonly Color Success = Color.FromArgb(31, 122, 83);
    internal static readonly Color Warning = Color.FromArgb(166, 104, 0);
    internal static readonly Color Error = Color.FromArgb(180, 35, 55);

    internal static Font UiFont(float size = 9.5F, FontStyle style = FontStyle.Regular) =>
        new("Segoe UI Variable Text", size, style);

    internal static Font DisplayFont(float size, FontStyle style = FontStyle.Bold) =>
        new("Segoe UI Variable Display", size, style);

    internal static void ApplyForm(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Text;
        form.Font = UiFont();
        form.Icon = AppIcon.Create();
        form.HandleCreated += (_, _) => ApplyWindowChrome(form.Handle);
        if (form.IsHandleCreated) ApplyWindowChrome(form.Handle);
    }

    internal static void StyleButton(Button button, bool primary = false, bool danger = false)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = danger ? Color.FromArgb(92, 45, 57) : Border;
        button.FlatAppearance.MouseOverBackColor = primary ? AccentHover : SurfaceHover;
        button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(132, 42, 25) : SurfaceRaised;
        button.BackColor = primary ? Accent : SurfaceRaised;
        button.ForeColor = primary ? Color.White : danger ? Error : Text;
        button.Font = UiFont(9.25F, FontStyle.Bold);
        button.Padding = new Padding(12, 2, 12, 3);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    internal static void StyleInput(Control control)
    {
        control.BackColor = SurfaceRaised;
        control.ForeColor = Text;
        control.Font = UiFont(9.5F);

        switch (control)
        {
            case TextBox textBox:
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ComboBox comboBox:
                comboBox.FlatStyle = FlatStyle.Flat;
                comboBox.IntegralHeight = false;
                comboBox.DropDownHeight = 280;
                break;
            case NumericUpDown numeric:
                numeric.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ListBox listBox:
                listBox.BorderStyle = BorderStyle.None;
                listBox.IntegralHeight = false;
                break;
        }
    }

    internal static Color StatusColor(Core.OverlayKind kind) => kind switch
    {
        Core.OverlayKind.Success => Success,
        Core.OverlayKind.Warning => Warning,
        Core.OverlayKind.Error => Error,
        _ => Accent
    };

    private static void ApplyWindowChrome(nint handle)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763)) return;
        var enabled = 0;
        _ = DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int));
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            var rounded = 2;
            _ = DwmSetWindowAttribute(handle, 33, ref rounded, sizeof(int));
            var border = ColorTranslator.ToWin32(Border);
            _ = DwmSetWindowAttribute(handle, 34, ref border, sizeof(int));
            var caption = ColorTranslator.ToWin32(Background);
            _ = DwmSetWindowAttribute(handle, 35, ref caption, sizeof(int));
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);
}
