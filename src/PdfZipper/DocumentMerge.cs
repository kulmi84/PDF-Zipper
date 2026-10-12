using System.Runtime.InteropServices;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfZipper;

public static class DocumentMerge
{
    public static bool IsWord(string path) => Path.GetExtension(path).Equals(".doc", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".docx", StringComparison.OrdinalIgnoreCase);
    public static bool WordAvailable => Type.GetTypeFromProgID("Word.Application", false) is not null;
    public static string[] Validate(string[] paths)
    {
        if (paths.Length < 2) throw new InvalidOperationException(Language.Text("Bitte mindestens zwei Dateien auswählen.", "Please select at least two files."));
        var full = paths.Select(Path.GetFullPath).ToArray();
        if (full.Distinct(StringComparer.OrdinalIgnoreCase).Count() != full.Length) throw new InvalidOperationException(Language.Text("Bitte unterschiedliche Dateien auswählen.", "Please select different files."));
        string[] extensions = [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"];
        if (full.Any(p => !File.Exists(p) || !extensions.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase))) throw new InvalidOperationException(Language.Text("Unterstützt werden PDF, Word (.doc/.docx), JPG und PNG.", "Supported files: PDF, Word (.doc/.docx), JPG and PNG."));
        if (full.Any(p => !string.Equals(Path.GetDirectoryName(full[0]), Path.GetDirectoryName(p), StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException(Language.Text("Alle Dateien müssen im selben Ordner liegen.", "All files must be in the same folder."));
        return full;
    }

    public static string Merge(string[] paths, Action<int, string>? progress = null)
    {
        var inputs = Validate(paths);
        if (inputs.Any(IsWord) && !WordAvailable) throw new InvalidOperationException(Language.Text("Für Worddateien wird installiertes Microsoft Word benötigt.", "Microsoft Word must be installed to convert Word documents."));
        var temporary = Path.Combine(Path.GetTempPath(), "PDF-Zipper-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            using var result = new PdfDocument();
            using var word = inputs.Any(IsWord) ? new WordConverter() : null;
            for (int i = 0; i < inputs.Length; i++)
            {
                var path = inputs[i];
                progress?.Invoke(i + 1, Path.GetFileName(path));
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext is ".jpg" or ".jpeg" or ".png") AddImage(result, path, Path.Combine(temporary, $"image-{i}.png"));
                else
                {
                    string pdf = path;
                    if (IsWord(path)) { pdf = Path.Combine(temporary, $"word-{i}.pdf"); word!.Export(path, pdf); }
                    using var input = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
                    if (input.SecuritySettings.IsEncrypted || input.PageCount == 0) throw new InvalidOperationException(Language.Text($"{Path.GetFileName(path)} enthält keine lesbaren, ungeschützten Seiten.", $"{Path.GetFileName(path)} has no readable, unprotected pages."));
                    foreach (var page in input.Pages) result.AddPage(page);
                }
            }
            return Zipper.Save(result, inputs[0], Language.Text("_zusammengeführt", "_merged"));
        }
        finally { Directory.Delete(temporary, true); }
    }

    private static void AddImage(PdfDocument result, string path, string normalized)
    {
        using (var image = System.Drawing.Image.FromFile(path))
        {
            if (image.PropertyIdList.Contains(0x112))
            {
                int orientation = BitConverter.ToUInt16(image.GetPropertyItem(0x112)!.Value!, 0);
                var rotation = orientation switch { 2 => RotateFlipType.RotateNoneFlipX, 3 => RotateFlipType.Rotate180FlipNone, 4 => RotateFlipType.Rotate180FlipX, 5 => RotateFlipType.Rotate90FlipX, 6 => RotateFlipType.Rotate90FlipNone, 7 => RotateFlipType.Rotate270FlipX, 8 => RotateFlipType.Rotate270FlipNone, _ => RotateFlipType.RotateNoneFlipNone };
                image.RotateFlip(rotation);
            }
            image.Save(normalized, System.Drawing.Imaging.ImageFormat.Png);
        }
        using var picture = XImage.FromFile(normalized);
        var page = result.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        if (picture.PixelWidth > picture.PixelHeight) page.Orientation = PdfSharp.PageOrientation.Landscape;
        const double margin = 28.35; // 10 mm; preserve proportions, one image per page.
        double scale = Math.Min((page.Width.Point - 2 * margin) / picture.PixelWidth, (page.Height.Point - 2 * margin) / picture.PixelHeight);
        double width = picture.PixelWidth * scale, height = picture.PixelHeight * scale;
        using var graphics = XGraphics.FromPdfPage(page);
        graphics.DrawImage(picture, (page.Width.Point - width) / 2, (page.Height.Point - height) / 2, width, height);
    }

    private sealed class WordConverter : IDisposable
    {
        private object? application;
        public WordConverter()
        {
            try
            {
                application = Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application", true)!);
                dynamic word = application!;
                word.Visible = false;
                word.DisplayAlerts = 0;
                word.AutomationSecurity = 3; // Disable document macros before opening any input.
                object options = word.Options;
                try { ((dynamic)options).UpdateLinksAtOpen = false; } finally { Marshal.FinalReleaseComObject(options); }
            }
            catch { Dispose(); throw; }
        }
        public void Export(string input, string output)
        {
            object? documents = null, document = null;
            try
            {
                documents = ((dynamic)application!).Documents;
                // A deliberately wrong password makes protected files fail without a password prompt.
                document = ((dynamic)documents).Open(FileName: input, ConfirmConversions: false, ReadOnly: true, AddToRecentFiles: false, PasswordDocument: Guid.NewGuid().ToString(), PasswordTemplate: Guid.NewGuid().ToString(), Visible: false, OpenAndRepair: false, NoEncodingDialog: true);
                ((dynamic)document).ExportAsFixedFormat(OutputFileName: output, ExportFormat: 17, OpenAfterExport: false);
            }
            catch (COMException e) { throw new InvalidOperationException(Language.Text($"Word konnte „{Path.GetFileName(input)}“ nicht als PDF exportieren. Prüfe die Datei und die Word-Aktivierung. Geschützte Dateien werden nicht unterstützt.", $"Word could not export '{Path.GetFileName(input)}' to PDF. Check the file and Word activation. Protected files are not supported."), e); }
            finally
            {
                if (document is not null) { try { ((dynamic)document).Close(SaveChanges: 0); } finally { Marshal.FinalReleaseComObject(document); } }
                if (documents is not null) Marshal.FinalReleaseComObject(documents);
            }
        }
        public void Dispose()
        {
            if (application is null) return;
            try { ((dynamic)application).Quit(SaveChanges: 0); }
            finally { Marshal.FinalReleaseComObject(application); application = null; }
        }
    }
}
