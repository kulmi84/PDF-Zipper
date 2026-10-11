using PdfZipper;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Security.Cryptography;
using System.Globalization;

foreach (var language in new[] { "de-DE", "en-US", "fr-FR" }) {
CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
var root = Path.Combine(Path.GetTempPath(), "pdf-zipper-test-" + Guid.NewGuid());
Directory.CreateDirectory(root);
int checks = 0;
void Assert(bool valid, string name) { if (!valid) throw new Exception(name); checks++; }
void Reject(Action action, string name) {
    bool failed = false;
    try { action(); } catch (Exception e) when (e is InvalidOperationException or PdfReaderException) { failed = true; }
    Assert(failed, name);
}
void Create(string path, params int[] widths) {
    using var pdf = new PdfDocument();
    foreach (var width in widths) { var page = pdf.AddPage(); page.Width = PdfSharp.Drawing.XUnit.FromPoint(width); }
    pdf.Save(path);
}
try {
    string front = Path.Combine(root, "Scan Vorderseiten.pdf"), back = Path.Combine(root, "Scan Rückseiten.pdf");
    Create(front, 101, 103, 105); Create(back, 106, 104, 102);
    var fhash = SHA256.HashData(File.ReadAllBytes(front)); var bhash = SHA256.HashData(File.ReadAllBytes(back));
    Assert(Zipper.DetectFront(front, back) == 0, "German roles");
    Assert(Zipper.DetectFront(back, front) == 1, "Reversed selection");
    Assert(Zipper.DetectFront("scan_front.pdf", "scan_back.pdf") == 0, "English roles");
    Assert(Zipper.DetectFront("confront.pdf", "backyard.pdf") is null, "No substring guesses");
    Assert(Zipper.DetectFront("front_back.pdf", "back.pdf") is null, "Ambiguous role");
    Assert(Zipper.DetectFront("front.pdf", "scan.pdf") is null, "Incomplete evidence");
    Assert(Zipper.DetectFront("en0018.pdf", "en0019.pdf") == 0, "Scanner counters");
    Assert(Zipper.DetectFront("en0019.pdf", "en0018.pdf") == 1, "Reversed scanner selection");
    Assert(Zipper.DetectFront("18.pdf", "19.pdf") == 0, "Numeric filenames");
    Assert(Zipper.DetectFront("18 Vorderseite Scan 1.pdf", "19 Rückseite Scan2.pdf") == 0, "User example with roles");
    Assert(Zipper.DetectFront("en0018.pdf", "en0020.pdf") is null, "Counter gap prompts");
    Assert(Zipper.DetectFront("en0018.pdf", "other0019.pdf") is null, "Different prefixes prompt");
    Assert(Zipper.DetectFront("scan18_1.pdf", "scan19_2.pdf") is null, "Multiple counters prompt");
    Assert(Zipper.DetectFront("front_18.pdf", "front_19.pdf") is null, "Role ambiguity beats counters");
    Assert(Zipper.DetectFront("19 Vorderseite.pdf", "18 Rückseite.pdf") == 0, "Explicit roles beat numbering");
    var output = Zipper.Merge(front, back);
    Assert(output.EndsWith(Language.IsGerman ? "_verzahnt.pdf" : "_interleaved.pdf"), "Localized output filename");
    try { Zipper.Validate([]); } catch (InvalidOperationException e) {
        Assert(e.Message == (language == "de-DE" ? "Bitte genau zwei PDF-Dateien markieren." : "Please select exactly two PDF files."), "Language and English fallback");
    }
    using (var pdf = PdfReader.Open(output, PdfDocumentOpenMode.Import)) {
        Assert(pdf.PageCount == 6, "Page count");
        for (int i = 0; i < 6; i++) Assert(Math.Abs(pdf.Pages[i].Width.Point - (101 + i)) < .01, "Order " + i);
    }
    var second = Zipper.Merge(front, back);
    Assert(output != second && File.Exists(output), "Collision does not overwrite");
    Assert(fhash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(front))) && bhash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(back))), "Inputs unchanged");
    Reject(() => Zipper.Validate([front]), "Exactly two inputs");
    Reject(() => Zipper.Validate([front, front]), "Different inputs");
    string shortBack = Path.Combine(root, "short.pdf"); Create(shortBack, 102);
    var joined = Zipper.Concatenate(front, shortBack);
    using (var pdf = PdfReader.Open(joined, PdfDocumentOpenMode.Import)) {
        Assert(pdf.PageCount == 4, "Concatenation allows unequal counts");
        var widths = new[] { 101, 103, 105, 102 };
        for (int i = 0; i < widths.Length; i++) Assert(Math.Abs(pdf.Pages[i].Width.Point - widths[i]) < .01, "Concatenation order " + i);
    }
    var reversed = Zipper.Concatenate(shortBack, front);
    using (var pdf = PdfReader.Open(reversed, PdfDocumentOpenMode.Import))
        Assert(Math.Abs(pdf.Pages[0].Width.Point - 102) < .01, "Swapped concatenation");
    Assert(Zipper.Concatenate(front, shortBack) != joined && File.Exists(joined), "Concatenation collision");
    var multiple = Zipper.Concatenate(shortBack, front, back);
    using (var pdf = PdfReader.Open(multiple, PdfDocumentOpenMode.Import)) {
        var widths = new[] { 102, 101, 103, 105, 106, 104, 102 };
        Assert(pdf.PageCount == widths.Length, "Three PDFs page count");
        for (int i = 0; i < widths.Length; i++) Assert(Math.Abs(pdf.Pages[i].Width.Point - widths[i]) < .01, "Three PDFs order " + i);
    }
    Reject(() => Zipper.Concatenate(front), "Concatenation minimum two");
    Reject(() => Zipper.Concatenate(front, back, front), "Duplicate among multiple inputs");
    Reject(() => Zipper.Validate([front, back, shortBack]), "Interleaving remains exactly two");
    Assert(fhash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(front))), "Concatenation preserves original");
    Reject(() => Zipper.Merge(front, shortBack), "Mismatched pages");
    var other = Path.Combine(root, "other"); Directory.CreateDirectory(other);
    var elsewhere = Path.Combine(other, "back.pdf"); Create(elsewhere, 102);
    Reject(() => Zipper.Merge(front, elsewhere), "Different directories");
    Reject(() => Zipper.Concatenate(front, back, elsewhere), "Multiple PDFs different directories");
    var broken = Path.Combine(root, "broken.pdf"); File.WriteAllText(broken, "invalid pdf");
    Reject(() => Zipper.Merge(front, broken), "Corrupt PDF");
    Reject(() => Zipper.Concatenate(front, broken), "Corrupt concatenation produces no output");
    Reject(() => Zipper.Concatenate(front, back, broken), "Corrupt third PDF");
    var tasks = Enumerable.Range(0, 4).Select(_ => Task.Run(() => Zipper.Merge(front, back))).ToArray();
    Task.WaitAll(tasks);
    Assert(tasks.Select(t => t.Result).Distinct().Count() == 4, "Concurrent collisions");
    Assert(!Directory.EnumerateFiles(root, "*.tmp").Any(), "No temporary leftovers");
    Console.WriteLine($"PASS: {checks} checks");
}
finally { Directory.Delete(root, true); }
}
