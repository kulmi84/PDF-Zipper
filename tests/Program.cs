using PdfZipper;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Security.Cryptography;

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
    var output = Zipper.Merge(front, back);
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
    Reject(() => Zipper.Merge(front, shortBack), "Mismatched pages");
    var other = Path.Combine(root, "other"); Directory.CreateDirectory(other);
    var elsewhere = Path.Combine(other, "back.pdf"); Create(elsewhere, 102);
    Reject(() => Zipper.Merge(front, elsewhere), "Different directories");
    var broken = Path.Combine(root, "broken.pdf"); File.WriteAllText(broken, "invalid pdf");
    Reject(() => Zipper.Merge(front, broken), "Corrupt PDF");
    var tasks = Enumerable.Range(0, 4).Select(_ => Task.Run(() => Zipper.Merge(front, back))).ToArray();
    Task.WaitAll(tasks);
    Assert(tasks.Select(t => t.Result).Distinct().Count() == 4, "Concurrent collisions");
    Assert(!Directory.EnumerateFiles(root, "*.tmp").Any(), "No temporary leftovers");
    Console.WriteLine($"PASS: {checks} checks");
}
finally { Directory.Delete(root, true); }
