namespace PdfZipper;

internal sealed class ConversionProgress : WindowsDialog
{
    public Exception? Error { get; private set; }
    private bool finished;
    public ConversionProgress(string[] paths, bool diagnostics = false)
    {
        Text = Language.Text("PDF-Zipper – Als PDF zusammenführen", "PDF-Zipper – Merge as PDF");
        ClientSize = new Size(390, 106);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false; MinimizeBox = false; ControlBox = false;
        var label = new Label { Dock = DockStyle.Top, Height = 40, Padding = new Padding(10), AutoEllipsis = true, Text = Language.Text("Dateien werden vorbereitet …", "Preparing files …") };
        var bar = new ProgressBar { Dock = DockStyle.Bottom, Height = 16, Style = ProgressBarStyle.Marquee };
        var phaseLabel = new Label { Dock = DockStyle.Fill, Padding = new Padding(10, 5, 10, 0), AutoEllipsis = true };
        Controls.Add(phaseLabel); Controls.Add(label); Controls.Add(bar);
        string phase = Language.Text("Dateien vorbereiten", "Prepare files");
        var phaseWatch = System.Diagnostics.Stopwatch.StartNew();
        var clock = new System.Windows.Forms.Timer { Interval = 1000 };
        Disposed += (_, _) => clock.Dispose();
        clock.Tick += (_, _) => phaseLabel.Text = $"{phase} … {phaseWatch.Elapsed.TotalSeconds:0} s";
        string? log = null;
        try
        {
            if (diagnostics)
            {
            string folder = Path.Combine(Path.GetTempPath(), "PDF-Zipper-Diagnose");
            Directory.CreateDirectory(folder);
            log = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.log");
            File.WriteAllText(log, $"PDF-Zipper 0.1.16 local diagnostics\nWindows: {Environment.OSVersion}; x64: {Environment.Is64BitProcess}\n");
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        void Log(string message)
        {
            if (log is null) return;
            try { File.AppendAllText(log, $"{DateTime.Now:HH:mm:ss.fff} {message}\n"); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
        FormClosing += (_, e) => { e.Cancel = !finished; };
        Shown += (_, _) =>
        {
            clock.Start();
            var worker = new Thread(() =>
            {
                try
                {
                    DocumentMerge.Merge(paths, (number, name) =>
                    {
                        Log($"FILE {number}/{paths.Length} {name}");
                        BeginInvoke(() => label.Text = $"{number}/{paths.Length}  {name}");
                    }, stage =>
                    {
                        Log(stage.Starting ? $"START {stage.Name}" : $"END {stage.Name}: {stage.ElapsedMilliseconds} ms");
                        if (stage.Starting) BeginInvoke(() => { phase = stage.Name; phaseWatch.Restart(); phaseLabel.Text = phase + " … 0 s"; });
                    });
                    Log("SUCCESS");
                }
                catch (Exception error) { Error = error; Log($"ERROR {error.GetType().Name} HRESULT=0x{error.HResult:X8}"); }
                finally { BeginInvoke(() => { clock.Stop(); finished = true; Close(); }); }
            });
            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();
        };
    }
}
