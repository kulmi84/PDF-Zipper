using System.Text.RegularExpressions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfZipper;

public static class Zipper
{
    private static readonly Regex Front = new(@"(^|[\W_])(front|fronts|vorderseite[n]?|vorne|recto)(?=$|[\W_])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Back = new(@"(^|[\W_])(back|backs|rückseite[n]?|rueckseite[n]?|hinten|verso)(?=$|[\W_])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex NumberedScan = new(@"^(?<prefix>[^0-9]*)(?<number>[0-9]+)(?<suffix>[^0-9]*)$", RegexOptions.CultureInvariant);

    // Explicit roles take precedence over consecutive scanner counters.
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
        // Incomplete or contradictory role words must still prompt the user.
        var firstName = Path.GetFileNameWithoutExtension(first);
        var secondName = Path.GetFileNameWithoutExtension(second);
        if (Front.IsMatch(firstName) || Back.IsMatch(firstName) || Front.IsMatch(secondName) || Back.IsMatch(secondName)) return null;
        var x = NumberedScan.Match(firstName);
        var y = NumberedScan.Match(secondName);
        if (!x.Success || !y.Success ||
            !string.Equals(x.Groups["prefix"].Value, y.Groups["prefix"].Value, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(x.Groups["suffix"].Value, y.Groups["suffix"].Value, StringComparison.OrdinalIgnoreCase)) return null;
        var n = System.Numerics.BigInteger.Parse(x.Groups["number"].Value, System.Globalization.CultureInfo.InvariantCulture);
        var m = System.Numerics.BigInteger.Parse(y.Groups["number"].Value, System.Globalization.CultureInfo.InvariantCulture);
        if (System.Numerics.BigInteger.Abs(n - m) == 1) return n < m ? 0 : 1;
        return null;
    }

    public static string[] Validate(string[] paths, bool concatenate = false)
    {
        if (concatenate ? paths.Length < 2 : paths.Length != 2) throw new InvalidOperationException(concatenate ? Language.Text("Bitte mindestens zwei PDF-Dateien markieren.", "Please select at least two PDF files.") : Language.Text("Bitte genau zwei PDF-Dateien markieren.", "Please select exactly two PDF files."));
        var full = paths.Select(Path.GetFullPath).ToArray();
        if (full.Distinct(StringComparer.OrdinalIgnoreCase).Count() != full.Length)
            throw new InvalidOperationException(Language.Text("Bitte unterschiedliche PDF-Dateien auswählen.", "Please select different PDF files."));
        foreach (var path in full)
            if (!File.Exists(path) || !string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(Language.Text("Alle Dateien müssen vorhandene PDF-Dateien sein.", "All files must be existing PDF files."));
        if (full.Any(path => !string.Equals(Path.GetDirectoryName(full[0]), Path.GetDirectoryName(path), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(Language.Text("Alle PDF-Dateien müssen im selben Ordner liegen.", "All PDF files must be in the same folder."));
        return full;
    }

    public static string Merge(string front, string back)
    {
        var inputs = Validate([front, back]);
        using var f = PdfReader.Open(inputs[0], PdfDocumentOpenMode.Import);
        using var b = PdfReader.Open(inputs[1], PdfDocumentOpenMode.Import);
        if (f.SecuritySettings.IsEncrypted || b.SecuritySettings.IsEncrypted)
            throw new InvalidOperationException(Language.Text("Geschützte PDFs werden nicht unterstützt. Bitte ungeschützte Kopien verwenden.", "Protected PDFs are not supported. Please use unprotected copies."));
        if (f.PageCount == 0 || f.PageCount != b.PageCount)
            throw new InvalidOperationException(Language.Text($"Die Seitenzahlen müssen gleich und größer als null sein (Vorderseiten: {f.PageCount}, Rückseiten: {b.PageCount}). Es wurde keine Ausgabe erstellt.", $"Page counts must be equal and greater than zero (front: {f.PageCount}, back: {b.PageCount}). No output was created."));
        using var result = new PdfDocument();
        result.Info.Title = Language.Text("Verzahnte Scans", "Interleaved scans");
        for (int i = 0; i < f.PageCount; i++)
        {
            result.AddPage(f.Pages[i]);
            result.AddPage(b.Pages[b.PageCount - 1 - i]);
        }
        return Save(result, inputs[0], Language.Text("_verzahnt", "_interleaved"));
    }

    public static string Concatenate(params string[] paths)
    {
        var inputs = Validate(paths, concatenate: true);
        using var result = new PdfDocument();
        result.Info.Title = Language.Text("Zusammengeführte PDFs", "Merged PDFs");
        foreach (var path in inputs)
        {
            using var input = PdfReader.Open(path, PdfDocumentOpenMode.Import);
            if (input.SecuritySettings.IsEncrypted || input.PageCount == 0)
                throw new InvalidOperationException(Language.Text("Bitte ungeschützte PDFs mit mindestens einer Seite verwenden.", "Please use unprotected PDFs with at least one page."));
            foreach (var page in input.Pages) result.AddPage(page);
        }
        return Save(result, inputs[0], Language.Text("_zusammengeführt", "_merged"));
    }

    internal static string Save(PdfDocument result, string first, string ending)
    {
        var directory = Path.GetDirectoryName(first)!;
        // Complete the PDF first; publish with a non-overwriting, same-directory move.
        string temporary = Path.Combine(directory, $".pdf-zipper-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                result.Save(stream, false);
                stream.Flush(true);
            }
            string stem = Path.GetFileNameWithoutExtension(first) + ending;
            for (int suffix = 0; suffix < 10000; suffix++)
            {
                var output = Path.Combine(directory, stem + (suffix == 0 ? "" : $" ({suffix})") + ".pdf");
                try { File.Move(temporary, output, false); return output; }
                catch (IOException) when (File.Exists(output)) { }
            }
            throw new IOException(Language.Text("Es konnte kein freier Ausgabename gefunden werden.", "No unused output filename could be found."));
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

