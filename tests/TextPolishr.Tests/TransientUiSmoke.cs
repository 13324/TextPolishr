using TextPolishr.Core;
using TextPolishr.UI;

namespace TextPolishr.Tests;

internal static class TransientUiSmoke
{
    internal static void Run()
    {
        using var menu = new ActionMenuForm(TransformAction.CreateDefaults());
        var chosen = false;
        menu.ActionChosen += (_, _) => chosen = true;
        menu.ShowAtCursor();
        Application.DoEvents();
        SaveIfRequested(menu, "preset-menu.png");
        menu.Hide();
        if (!menu.HandleKey(Keys.D1) || !chosen)
        {
            throw new InvalidOperationException("Preset menu keyboard selection failed.");
        }

        using var custom = new CustomInstructionForm();
        custom.Show();
        Application.DoEvents();
        SaveIfRequested(custom, "custom-instruction.png");
        custom.Hide();

        using var overlay = new OverlayForm();
        overlay.ShowStatus("Text replaced · Improve", OverlayKind.Success, 100);
        SaveIfRequested(overlay, "status-overlay.png");
        overlay.Hide();
    }

    private static void SaveIfRequested(Form form, string fileName)
    {
        var directory = Environment.GetEnvironmentVariable("TEXTPOLISHR_TRANSIENT_SNAPSHOT_DIR");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, form.ClientRectangle);
        bitmap.Save(Path.Combine(directory, fileName), System.Drawing.Imaging.ImageFormat.Png);
    }
}
