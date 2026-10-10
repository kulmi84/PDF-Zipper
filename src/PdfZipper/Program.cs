namespace PdfZipper;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        try
        {
            bool concatenate = args.Length > 0 && args[0] == "--merge";
            var paths = Zipper.Validate(concatenate ? args[1..] : args, concatenate);
            if (concatenate)
            {
                using var order = new OrderChoice(paths);
                if (order.ShowDialog() != DialogResult.OK) return 0;
                Zipper.Concatenate(order.OrderedPaths);
                return 0;
            }
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
            using var dialog = new ErrorDialog(message);
            dialog.ShowDialog();
            return 1;
        }
    }
}

internal sealed class OrderChoice : WindowsDialog
{
    private readonly List<string> ordered;
    public string[] OrderedPaths => ordered.ToArray();
    public OrderChoice(string[] paths)
    {
        ordered = paths.ToList();
        Text = "PDF-Zipper – Reihenfolge wählen";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(390, 124 + Math.Min(paths.Length, 8) * 20);



        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false; MinimizeBox = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.Controls.Add(new Label { Text = "Reihenfolge der PDFs:", Dock = DockStyle.Fill });
        var list = new ListBox { Dock = DockStyle.Fill, BackColor = SurfaceColor, ForeColor = ForeColor, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, HorizontalScrollbar = true };
        void RefreshOrder(int selected)
        {
            list.BeginUpdate(); list.Items.Clear();
            for (int i = 0; i < ordered.Count; i++) list.Items.Add($"{i + 1}   {Path.GetFileName(ordered[i])}");
            list.SelectedIndex = selected; list.EndUpdate();
        }
        layout.Controls.Add(list);
        Button MakeButton(string text) => CreateButton(text);
        var moves = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false };
        var up = MakeButton("Nach oben"); var down = MakeButton("Nach unten");
        void Move(int offset)
        {
            int from = list.SelectedIndex, to = from + offset;
            if (from < 0 || to < 0 || to >= ordered.Count) return;
            (ordered[from], ordered[to]) = (ordered[to], ordered[from]);
            RefreshOrder(to);
        }
        up.Click += (_, _) => Move(-1); down.Click += (_, _) => Move(1);
        list.SelectedIndexChanged += (_, _) => { up.Enabled = list.SelectedIndex > 0; down.Enabled = list.SelectedIndex >= 0 && list.SelectedIndex < ordered.Count - 1; };
        moves.Controls.Add(up); moves.Controls.Add(down); layout.Controls.Add(moves);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false, FlowDirection = FlowDirection.RightToLeft };
        var merge = MakeButton("Zusammenführen"); merge.DialogResult = DialogResult.OK;
        var cancel = MakeButton("Abbrechen"); cancel.DialogResult = DialogResult.Cancel;
        actions.Controls.Add(merge); actions.Controls.Add(cancel);
        layout.Controls.Add(actions); Controls.Add(layout);
        AcceptButton = merge; CancelButton = cancel;
        RefreshOrder(0);
    }
}
internal sealed class FrontChoice : WindowsDialog
{
    public int FrontIndex { get; private set; }
    public FrontChoice(string[] paths)
    {
        Text = "PDF-Zipper – Vorderseiten auswählen";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(390, 156);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false; MinimizeBox = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.Controls.Add(new Label { Text = "Welche Datei enthält die Vorderseiten?\nDie andere Datei wird rückwärts eingefügt.", AutoSize = true });
        for (int i = 0; i < 2; i++)
        {
            int index = i;
            var button = CreateButton(Path.GetFileName(paths[i]));
            button.Dock = DockStyle.Fill; button.AutoEllipsis = true;
            button.Click += (_, _) => { FrontIndex = index; DialogResult = DialogResult.OK; };
            layout.Controls.Add(button);
        }
        var cancel = CreateButton("Abbrechen"); cancel.DialogResult = DialogResult.Cancel;
        CancelButton = cancel; layout.Controls.Add(cancel); Controls.Add(layout);
    }
}



