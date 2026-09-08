using System.Drawing.Drawing2D;

namespace TextPolishr.UI;

internal sealed class CustomInstructionForm : Form
{
    private readonly TextBox _instruction;
    private readonly Panel? _instructionHost;

    public CustomInstructionForm()
    {
        Text = "Custom instruction";
        FormBorderStyle = FormBorderStyle.None;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(600, 260);
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Padding = new Padding(1);
        TopMost = true;

        var mark = new CatLogo
        {
            Location = new Point(24, 17),
            Size = new Size(38, 38)
        };
        var eyebrow = new Label
        {
            Text = "CUSTOM INSTRUCTION",
            AutoSize = true,
            Location = new Point(76, 18),
            Font = Theme.UiFont(7.75F, FontStyle.Bold),
            ForeColor = Theme.Accent
        };
        var title = new Label
        {
            Text = "Describe the requested change",
            AutoSize = true,
            Location = new Point(74, 35),
            Font = Theme.DisplayFont(15F, FontStyle.Bold),
            ForeColor = Theme.Text
        };
        var hint = new Label
        {
            Text = "The selected text is sent with this instruction and replaced when the request succeeds.",
            AutoSize = true,
            Location = new Point(25, 78),
            Font = Theme.UiFont(8.75F),
            ForeColor = Theme.Muted
        };
        _instructionHost = new Panel
        {
            Location = new Point(25, 108),
            Size = new Size(550, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Theme.Border,
            Padding = new Padding(1)
        };
        _instruction = new TextBox
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.SurfaceRaised,
            ForeColor = Theme.Text,
            Font = Theme.UiFont(10F),
            Padding = new Padding(12, 9, 12, 8)
        };
        _instructionHost.Controls.Add(_instruction);

        var apply = new Button { Text = "Apply", DialogResult = DialogResult.OK, Location = new Point(435, 191), Size = new Size(140, 42) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(321, 191), Size = new Size(104, 42) };
        Theme.StyleButton(apply, primary: true);
        Theme.StyleButton(cancel);
        Round(apply, 9);
        Round(cancel, 9);
        AcceptButton = apply;
        CancelButton = cancel;
        Controls.AddRange([mark, eyebrow, title, hint, _instructionHost, apply, cancel]);
        Round(_instructionHost!, 8);
    }

    public string Instruction => _instruction.Text.Trim();

    protected override CreateParams CreateParams
    {
        get
        {
            const int csDropShadow = 0x00020000;
            var parameters = base.CreateParams;
            parameters.ClassStyle |= csDropShadow;
            return parameters;
        }
    }

    protected override void OnShown(EventArgs eventArgs)
    {
        base.OnShown(eventArgs);
        _instruction.Focus();
    }

    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        using var path = Geometry.RoundRect(ClientRectangle, 16);
        Region = new Region(path);
        if (_instructionHost is not null) Round(_instructionHost, 8);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var border = new Pen(Theme.Border);
        using var path = Geometry.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 16);
        eventArgs.Graphics.DrawPath(border, path);
    }

    private static void Round(Control control, int radius)
    {
        using var path = Geometry.RoundRect(control.ClientRectangle, radius);
        control.Region = new Region(path);
    }
}
