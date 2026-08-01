using Xunit;

// Test classes run in parallel by default, and a measurement cannot. What an analyzer allocates has to be
// counted process-wide — its work is scheduled, so the per-thread counter misses the part that matters and
// reports the same figure whether the rule is cheap or not — and a process-wide count includes whatever a
// test in another class allocated at the same moment. That noise is added, never subtracted, so a ceiling
// under it fails on scheduling rather than on a regression.
//
// Serialising the assembly is the fix, and it is nearly free here: the suite runs in about a second, and
// nothing in it is slow enough for parallelism to be what makes it bearable.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
