namespace PdfZipper;

internal sealed class ConversionProgress : WindowsDialog
{
    public Exception? Error { get; private set; }
    private bool finished;
    public ConversionProgress(string[] paths)
    {
        Text = Language.Text("PDF-Zipper – Als PDF zusammenführen", "PDF-Zipper – Merge as PDF");
        ClientSize = new Size(390, 78);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false; MinimizeBox = false; ControlBox = false;
        var label = new Label { Dock = DockStyle.Top, Height = 40, Padding = new Padding(10), AutoEllipsis = true, Text = Language.Text("Dateien werden vorbereitet …", "Preparing files …") };
        var bar = new ProgressBar { Dock = DockStyle.Bottom, Height = 16, Style = ProgressBarStyle.Marquee };
        Controls.Add(label); Controls.Add(bar);
        FormClosing += (_, e) => { e.Cancel = !finished; };
        Shown += (_, _) =>
        {
            var worker = new Thread(() =>
            {
                try { DocumentMerge.Merge(paths, (number, name) => BeginInvoke(() => label.Text = $"{number}/{paths.Length}  {name}")); }
                catch (Exception error) { Error = error; }
                finally { BeginInvoke(() => { finished = true; Close(); }); }
            });
            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();
        };
    }
}
