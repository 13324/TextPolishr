using System.Runtime.InteropServices;

namespace TextPolishr.UI;

internal static class Theme
{
    // Carbon + Signal Vermilion. Warm, editorial and deliberately non-gradient.
    internal static readonly Color Background = Color.FromArgb(14, 14, 13);
    internal static readonly Color Sidebar = Color.FromArgb(18, 18, 16);
    internal static readonly Color Surface = Color.FromArgb(25, 25, 22);
    internal static readonly Color SurfaceRaised = Color.FromArgb(31, 31, 27);
    internal static readonly Color SurfaceHover = Color.FromArgb(42, 41, 36);
    internal static readonly Color Border = Color.FromArgb(53, 52, 46);
    internal static readonly Color BorderSoft = Color.FromArgb(39, 39, 34);
    internal static readonly Color Text = Color.FromArgb(246, 244, 238);
    internal static readonly Color Muted = Color.FromArgb(161, 159, 150);
    internal static readonly Color Subtle = Color.FromArgb(107, 105, 98);
    internal static readonly Color Accent = Color.FromArgb(242, 83, 55);
    internal static readonly Color AccentHover = Color.FromArgb(255, 105, 77);
    internal static readonly Color AccentSoft = Color.FromArgb(65, 31, 24);
    internal static readonly Color Success = Color.FromArgb(74, 222, 128);
    internal static readonly Color Warning = Color.FromArgb(251, 191, 36);
    internal static readonly Color Error = Color.FromArgb(251, 113, 133);

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
        button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(211, 65, 40) : SurfaceRaised;
        button.BackColor = primary ? Accent : SurfaceRaised;
        button.ForeColor = danger ? Error : Text;
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
        var enabled = 1;
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
