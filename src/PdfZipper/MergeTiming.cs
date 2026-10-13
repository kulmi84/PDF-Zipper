using System.Diagnostics;

namespace PdfZipper;

public sealed record MergeStage(string Name, bool Starting, long ElapsedMilliseconds);

internal sealed class MergeTiming : IDisposable
{
    private readonly string name;
    private readonly Action<MergeStage>? notify;
    private readonly Stopwatch watch;
    public MergeTiming(string name, Action<MergeStage>? notify)
    {
        this.name = name; this.notify = notify;
        notify?.Invoke(new(name, true, 0));
        watch = Stopwatch.StartNew();
    }
    public void Dispose() => notify?.Invoke(new(name, false, watch.ElapsedMilliseconds));
}
