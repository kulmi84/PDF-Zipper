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
    var imagePath = Path.Combine(root, "picture.png");
    using (var bitmap = new System.Drawing.Bitmap(400, 200)) {
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap)) graphics.Clear(System.Drawing.Color.Red);
        bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
    }
    var imageHash = SHA256.HashData(File.ReadAllBytes(imagePath));
    var stages = new List<MergeStage>();
    var mixed = DocumentMerge.Merge([front, imagePath, back], stages: stages.Add);
    Assert(stages.Count == 8 && stages.Where(s => !s.Starting).All(s => s.ElapsedMilliseconds >= 0), "All mixed processing stages timed");
    Assert(stages.Where((s,i) => i%2 == 0).All(s => s.Starting) && stages.Where((s,i) => i%2 == 1).All(s => !s.Starting), "Stage starts and completions paired");
    using (var merged = PdfReader.Open(mixed, PdfDocumentOpenMode.Import)) {
        Assert(merged.PageCount == 7, "PDF/image total pages");
        Assert(merged.Pages[0].Width.Point == 101 && merged.Pages[4].Width.Point == 106, "Mixed ordering preserved");
        Assert(merged.Pages[3].Width.Point > merged.Pages[3].Height.Point, "Landscape image A4");
    }
    Assert(imageHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(imagePath))), "Image original preserved");
    Assert(fhash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(front))), "Mixed PDF original preserved");
    Assert(DocumentMerge.Merge([front, imagePath, back]) != mixed, "Mixed collision safe");
    Reject(() => DocumentMerge.Validate([imagePath, imagePath]), "Mixed duplicate rejected");
    Reject(() => DocumentMerge.Validate([front, elsewhere]), "Mixed folders rejected");
    var unsupported = Path.Combine(root, "unsupported.txt"); File.WriteAllText(unsupported, "text");
    Reject(() => DocumentMerge.Validate([front, unsupported]), "Unsupported format rejected");
    int before = Directory.GetFiles(root, "*.pdf").Length;
    Reject(() => DocumentMerge.Merge([imagePath, broken]), "Failed mixed merge rejected");
    Assert(Directory.GetFiles(root, "*.pdf").Length == before, "Failed conversion produces no PDF");
    if (language == "de-DE" && Environment.GetEnvironmentVariable("PDF_ZIPPER_TEST_WORD") == "1" && DocumentMerge.WordAvailable) {
        var docx = Path.Combine(root, "sample.docx");
        using (var archive = System.IO.Compression.ZipFile.Open(docx, System.IO.Compression.ZipArchiveMode.Create)) {
            void Entry(string name, string xml) { using var writer = new StreamWriter(archive.CreateEntry(name).Open()); writer.Write(xml); }
            Entry("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
            Entry("_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
            Entry("word/document.xml", "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p><w:r><w:t>PDF-Zipper Word test page 1</w:t><w:br w:type=\"page\"/><w:t>PDF-Zipper Word test page 2</w:t></w:r></w:p><w:sectPr/></w:body></w:document>");
        }
        var docHash = SHA256.HashData(File.ReadAllBytes(docx));
        string? wordOutput = null; Exception? wordError = null;
        var thread = new Thread(() => { try { wordOutput = DocumentMerge.Merge([docx, imagePath, back]); } catch (Exception e) { wordError = e; } });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        if (!thread.Join(TimeSpan.FromMinutes(2))) throw new Exception("Word conversion test timed out");
        if (wordError is not null) throw wordError;
        using var wordPdf = PdfReader.Open(wordOutput!, PdfDocumentOpenMode.Import);
        Assert(wordPdf.PageCount == 6, "Word image PDF merge page count");
        Assert(wordPdf.Pages[3].Width.Point == 106, "Word mixed order");
        Assert(docHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(docx))), "Word original unchanged");
    }
    Console.WriteLine($"PASS: {checks} checks");
}
finally { Directory.Delete(root, true); }
}
