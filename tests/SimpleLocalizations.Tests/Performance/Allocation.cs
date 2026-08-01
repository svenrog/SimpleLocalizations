namespace SimpleLocalizations.Tests.Performance;

/// <summary>
/// What one call allocates, averaged over enough of them to drown the noise.
/// <para>
/// Bytes rather than a clock, because a shared build runner has no stable clock and a test that fails on a
/// busy machine gets disabled rather than fixed. Allocation is the thing worth holding still anyway: it is
/// what a lookup on a render path costs a server under load, and it moves for a reason.
/// </para>
/// </summary>
internal static class Allocation
{
    /// <summary>
    /// The mean bytes <paramref name="work"/> allocates per call. Warmed first, so a tiered-JIT recompile
    /// and whatever a <c>ResourceManager</c> loads on its first lookup are not counted as per-call cost.
    /// </summary>
    public static long PerCall(Action work, int calls = 1_000)
    {
        for (var warmup = 0; warmup < 100; warmup++)
        {
            work();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var call = 0; call < calls; call++)
        {
            work();
        }

        return (GC.GetAllocatedBytesForCurrentThread() - before) / calls;
    }
}
