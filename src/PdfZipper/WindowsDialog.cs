using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace PdfZipper;

// One Windows theme policy for every dialog, including high contrast.
internal class WindowsDialog : Form
{
    private readonly bool dark;
    protected Color SurfaceColor => dark ? Color.FromArgb(43, 43, 43) : SystemColors.Window;
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    public WindowsDialog()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        dark = !SystemInformation.HighContrast && key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        BackColor = dark ? Color.FromArgb(43, 43, 43) : SystemColors.Control;
        ForeColor = dark ? Color.FromArgb(242, 242, 242) : SystemColors.ControlText;
        Font = new Font("Segoe UI", 9);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false; MinimizeBox = false; ShowIcon = false; ShowInTaskbar = false;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int value = dark ? 1 : 0;
        DwmSetWindowAttribute(Handle, 20, ref value, sizeof(int));
    }

    protected Button CreateButton(string text)
    {
        var button = new RoundedButton(dark) { Text = text, Font = Font, Size = new Size(Math.Max(68, TextRenderer.MeasureText(text, Font).Width + 16), 24), Margin = new Padding(0, 2, 6, 2), BackColor = SurfaceColor, ForeColor = ForeColor };
        return button;
    }
}

internal sealed class WelcomeDialog : WindowsDialog
{
    public WelcomeDialog()
    {
        Text = "PDF-Zipper";
        ShowInTaskbar = true;
        ClientSize = new Size(390, 174);
        Controls.Add(new Label
        {
            Text = Language.Text("Dateien direkt im Windows-Explorer bearbeiten:\n\n" +
                "Mindestens zwei Dateien im selben Ordner markieren.\n" +
                "Rechtsklick → PDF-Zipper → Funktion auswählen.\n\n" +
                "Verzahnen: genau zwei PDFs. Zusammenführen: zwei oder mehr.\n" +
                "Auch Word, JPG und PNG als PDF zusammenführen.\nWorddateien benötigen installiertes Microsoft Word.", "Work with files directly in Windows Explorer:\n\nSelect at least two files in the same folder.\nRight-click → PDF-Zipper → choose an action.\n\nInterleave: exactly two PDFs. Merge: two or more.\nAlso merge Word, JPG and PNG as PDF.\nWord documents require installed Microsoft Word."),
            Location = new Point(12, 12), Size = new Size(366, 130)
        });
        var close = CreateButton(Language.Text("Schließen", "Close"));
        close.DialogResult = DialogResult.OK;
        close.Location = new Point(ClientSize.Width - close.Width - 12, ClientSize.Height - 34);
        Controls.Add(close);
        AcceptButton = close; CancelButton = close;
    }
}

internal sealed class ErrorDialog : WindowsDialog
{
    public ErrorDialog(string message)
    {
        Text = "PDF-Zipper";
        var label = new Label { Text = message, AutoSize = true, MaximumSize = new Size(360, 0), Location = new Point(12, 12) };
        Controls.Add(label);
        int height = Math.Max(92, label.PreferredHeight + 58);
        ClientSize = new Size(384, height);
        var ok = CreateButton("OK"); ok.DialogResult = DialogResult.OK;
        ok.Location = new Point(ClientSize.Width - ok.Width - 12, ClientSize.Height - 35);
        Controls.Add(ok); AcceptButton = ok; CancelButton = ok;
    }
}


internal sealed class RoundedButton : Button
{
    private readonly bool dark;
    private bool hovered, pressed;
    public RoundedButton(bool dark)
    {
        this.dark = dark;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        UseVisualStyleBackColor = false;
    }
    protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovered = pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { pressed = e.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        float radius = Math.Max(3, DeviceDpi / 96f * 3), diameter = radius * 2;
        var bounds = new RectangleF(.5f, .5f, Width - 1, Height - 1);
        using var shape = new System.Drawing.Drawing2D.GraphicsPath();
        shape.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        shape.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        shape.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        shape.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        shape.CloseFigure();
        // Flat Windows-menu-like surfaces: only hover and press change the fill.
        int shade = !Enabled ? 43 : pressed ? 48 : hovered ? 57 : 43;
        Color fill = dark ? Color.FromArgb(shade, shade, shade)
            : pressed ? SystemColors.ControlLight : hovered ? SystemColors.ControlLightLight : SystemColors.Control;
        Color border = dark ? Color.FromArgb(Enabled ? 63 : 52, Enabled ? 63 : 52, Enabled ? 63 : 52)
            : Enabled ? Color.FromArgb(210, 210, 210) : SystemColors.ControlLight;
        Color text = Enabled ? ForeColor : dark ? Color.FromArgb(145, 145, 145) : SystemColors.GrayText;
        if (SystemInformation.HighContrast) { fill = SystemColors.Control; border = SystemColors.ControlText; text = Enabled ? SystemColors.ControlText : SystemColors.GrayText; }
        using var brush = new SolidBrush(fill); using var pen = new Pen(border);
        e.Graphics.FillPath(brush, shape); e.Graphics.DrawPath(pen, shape);
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(5, 0, Width - 10, Height), text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -4, -4), text, fill);
    }
}

