using TextPolishr.Core;

namespace TextPolishr.UI;

internal sealed class ActionMenuForm : Form
{
    private readonly List<ActionRow> _rows = [];
    private int _selectedIndex;

    public ActionMenuForm(IReadOnlyList<TransformAction> actions)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Theme.Surface;
        Padding = new Padding(10);

        var visibleActions = actions.Where(action => action.ShowInMenu).ToArray();
        ClientSize = new Size(356, 112 + (visibleActions.Length + 2) * 50);

        var brand = new PillLabel
        {
            Text = "TEXT POLISHR",
            Location = new Point(16, 15),
            Size = new Size(104, 24)
        };
        var title = new Label
        {
            Text = "Select a preset",
            ForeColor = Theme.Text,
            Font = Theme.DisplayFont(15F, FontStyle.Bold),
            Location = new Point(16, 50),
            AutoSize = true
        };
        Controls.AddRange([brand, title]);

        var top = 84;
        for (var index = 0; index < visibleActions.Length; index++)
        {
            var action = visibleActions[index];
            AddRow((index + 1).ToString(), action.Name, top, () => ActionChosen?.Invoke(this, action));
            top += 50;
        }
        AddRow("C", "Custom instruction", top, () => CustomChosen?.Invoke(this, EventArgs.Empty));
        top += 50;
        AddRow("Esc", "Cancel", top, () => Cancelled?.Invoke(this, EventArgs.Empty), muted: true);

        SetSelection(0);
    }

    public event EventHandler<TransformAction>? ActionChosen;
    public event EventHandler? CustomChosen;
    public event EventHandler? Cancelled;

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

    public void ShowAtCursor()
    {
        var cursor = Cursor.Position;
        var area = Screen.GetWorkingArea(cursor);
        var x = Math.Min(cursor.X + 14, area.Right - Width - 8);
        var y = Math.Min(cursor.Y + 18, area.Bottom - Height - 8);
        Location = new Point(Math.Max(area.Left + 8, x), Math.Max(area.Top + 8, y));
        Show();
    }

    public bool HandleKey(Keys key)
    {
        if (key == Keys.Escape)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
            return true;
        }
        if (key == Keys.Up)
        {
            SetSelection((_selectedIndex - 1 + _rows.Count) % _rows.Count);
            return true;
        }
        if (key == Keys.Down)
        {
            SetSelection((_selectedIndex + 1) % _rows.Count);
            return true;
        }
        if (key == Keys.Enter)
        {
            _rows[_selectedIndex].PerformClick();
            return true;
        }
        if (key is >= Keys.D1 and <= Keys.D9)
        {
            var index = key - Keys.D1;
            if (index < _rows.Count - 2) _rows[index].PerformClick();
            return true;
        }
        if (key == Keys.C)
        {
            _rows[^2].PerformClick();
            return true;
        }
        return false;
    }

    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        using var path = Geometry.RoundRect(ClientRectangle, 18);
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        using var pen = new Pen(Theme.Border);
        using var path = Geometry.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 18);
        eventArgs.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        eventArgs.Graphics.DrawPath(pen, path);
    }

    private void AddRow(string key, string label, int top, Action click, bool muted = false)
    {
        var row = new ActionRow
        {
            KeyText = key,
            LabelText = label,
            Muted = muted,
            Location = new Point(16, top),
            Width = 324
        };
        row.Click += (_, _) => click();
        row.MouseEnter += (_, _) => SetSelection(_rows.IndexOf(row));
        _rows.Add(row);
        Controls.Add(row);
    }

    private void SetSelection(int index)
    {
        _selectedIndex = Math.Clamp(index, 0, _rows.Count - 1);
        for (var i = 0; i < _rows.Count; i++) _rows[i].IsSelected = i == _selectedIndex;
    }
}
