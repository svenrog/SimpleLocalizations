namespace SimpleLocalizations.Tests;

/// <summary>
/// The classes that read or set ambient culture, run one at a time.
/// <para>
/// Culture is process- and thread-wide state, and xUnit runs test classes in parallel by default: a test
/// that sets <c>DefaultThreadCurrentUICulture</c> otherwise decides what a test in another class reads, and
/// the suite passes or fails on scheduling. Serialising them is the fix — a lock around each read would still
/// leave the ambient default whatever ran last.
/// </para>
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CultureCollection
{
    public const string Name = "ambient culture";
}
