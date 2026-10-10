using System.Text.RegularExpressions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfZipper;

public static class Zipper
{
    private static readonly Regex Front = new(@"(^|[\W_])(front|fronts|vorderseite[n]?|vorne|recto)(?=$|[\W_])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Back = new(@"(^|[\W_])(back|backs|rückseite[n]?|rueckseite[n]?|hinten|verso)(?=$|[\W_])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // Return the front index only if the two filenames give consistent evidence.
    public static int? DetectFront(string first, string second)
    {
        int Role(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            bool f = Front.IsMatch(name), b = Back.IsMatch(name);
            return f == b ? 0 : f ? 1 : -1;
        }
        var a = Role(first); var b = Role(second);
        if (a == 1 && b == -1) return 0;
        if (a == -1 && b == 1) return 1;
        return null;
    }

    public static string[] Validate(string[] paths)
    {
        if (paths.Length != 2) throw new InvalidOperationException("Bitte genau zwei PDF-Dateien markieren.");
        var full = paths.Select(Path.GetFullPath).ToArray();
        if (string.Equals(full[0], full[1], StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Bitte zwei unterschiedliche PDF-Dateien auswählen.");
        foreach (var path in full)
            if (!File.Exists(path) || !string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Beide Dateien müssen vorhandene PDF-Dateien sein.");
        if (!string.Equals(Path.GetDirectoryName(full[0]), Path.GetDirectoryName(full[1]), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Beide PDF-Dateien müssen im selben Ordner liegen.");
        return full;
    }

    public static string Merge(string front, string back)
    {
        var inputs = Validate([front, back]);
        using var f = PdfReader.Open(inputs[0], PdfDocumentOpenMode.Import);
        using var b = PdfReader.Open(inputs[1], PdfDocumentOpenMode.Import);
        if (f.SecuritySettings.IsEncrypted || b.SecuritySettings.IsEncrypted)
            throw new InvalidOperationException("Geschützte PDFs werden nicht unterstützt. Bitte ungeschützte Kopien verwenden.");
        if (f.PageCount == 0 || f.PageCount != b.PageCount)
            throw new InvalidOperationException($"Die Seitenzahlen müssen gleich und größer als null sein (Vorderseiten: {f.PageCount}, Rückseiten: {b.PageCount}). Es wurde keine Ausgabe erstellt.");
        using var result = new PdfDocument();
        result.Info.Title = "Verzahnte Scans";
        for (int i = 0; i < f.PageCount; i++)
        {
            result.AddPage(f.Pages[i]);
            result.AddPage(b.Pages[b.PageCount - 1 - i]);
        }
        var directory = Path.GetDirectoryName(inputs[0])!;
        // Complete the PDF first; publish with a non-overwriting, same-directory move.
        string temporary = Path.Combine(directory, $".pdf-zipper-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                result.Save(stream, false);
                stream.Flush(true);
            }
            string stem = Path.GetFileNameWithoutExtension(inputs[0]) + "_verzahnt";
            for (int suffix = 0; suffix < 10000; suffix++)
            {
                var output = Path.Combine(directory, stem + (suffix == 0 ? "" : $" ({suffix})") + ".pdf");
                try { File.Move(temporary, output, false); return output; }
                catch (IOException) when (File.Exists(output)) { }
            }
            throw new IOException("Es konnte kein freier Ausgabename gefunden werden.");
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
