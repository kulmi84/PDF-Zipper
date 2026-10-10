namespace PdfZipper;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        try
        {
            var paths = Zipper.Validate(args);
            var front = Zipper.DetectFront(paths[0], paths[1]);
            if (front is null)
            {
                using var choice = new FrontChoice(paths);
                if (choice.ShowDialog() != DialogResult.OK) return 0;
                front = choice.FrontIndex;
            }
            Zipper.Merge(paths[front.Value], paths[1 - front.Value]);
            // Successful invocations finish without another click.
            return 0;
        }
        catch (Exception error)
        {
            string message = error is PdfSharp.Pdf.IO.PdfReaderException
                ? "Die PDF konnte nicht gelesen werden. Sie ist möglicherweise beschädigt oder passwortgeschützt."
                : error is UnauthorizedAccessException
                ? "Keine Schreib- oder Leseberechtigung für diesen Ordner oder die Dateien."
                : error.Message;
            MessageBox.Show(message, "PDF-Zipper", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}

internal sealed class FrontChoice : Form
{
    public int FrontIndex { get; private set; }
    public FrontChoice(string[] paths)
    {
        Text = "PDF-Zipper – Vorderseiten auswählen";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(540, 190);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false; MinimizeBox = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 4 };
        layout.Controls.Add(new Label { Text = "Welche Datei enthält die Vorderseiten?\nDie andere Datei wird rückwärts eingefügt.", AutoSize = true });
        for (int i = 0; i < 2; i++)
        {
            int index = i;
            var button = new Button { Text = Path.GetFileName(paths[i]), Dock = DockStyle.Fill, AutoEllipsis = true, Height = 36 };
            button.Click += (_, _) => { FrontIndex = index; DialogResult = DialogResult.OK; };
            layout.Controls.Add(button);
        }
        var cancel = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, AutoSize = true };
        CancelButton = cancel; layout.Controls.Add(cancel); Controls.Add(layout);
    }
}
