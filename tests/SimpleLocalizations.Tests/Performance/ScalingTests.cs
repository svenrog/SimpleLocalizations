using SimpleLocalizations.Generator;
using static SimpleLocalizations.Tests.VocabularyHarness;

namespace SimpleLocalizations.Tests.Performance;

/// <summary>
/// What the build half costs on a vocabulary the size of a real one. A component that is quick on the twenty
/// keys a test authors and slow on the six hundred a project authors is one nobody finds until a build drags,
/// and by then the cause is a rule nobody suspects.
/// <para>
/// Two different questions, which need two different measurements. Whether the work is <em>superlinear</em>
/// is a ratio between sizes: quadruple the input, and linear work lands near four. Whether the work is
/// <em>done more times than it needs to be</em> is not — a rule that re-reads a fixed-size set once per key
/// is still linear in keys, just several times steeper, and every ratio it produces looks exactly like the
/// cheap one. Only an absolute figure sees that, so that is what the analyzer rules are held to.
/// </para>
/// <para>
/// Allocation rather than elapsed time, and the compilation is built once and reused: building one dwarfs
/// running a rule over it, so including it would measure Roslyn instead.
/// </para>
/// </summary>
public class ScalingTests
{
    /// <summary>Comfortably above linear, comfortably below quadratic.</summary>
    private const double _linearEnough = 6.0;

    /// <summary>
    /// Above what a rule reading its set once costs over four hundred keys, and below what reading it per key
    /// costs — which is the whole job of the figure, and is checked by breaking the caching on purpose rather
    /// than assumed. Room enough that a framework moving underneath it is not a failure, and no more.
    /// </summary>
    private const long _onePass = 900_000;

    private static Declaration Declared(string keyType = "SimpleLocalizations.LocalizationKey") =>
        new("ProbeKeys", keyType, "Probe");

    private static string Vocabulary(int keys) =>
        Keys([.. Enumerable.Range(0, keys).Select(index => $"section{index % 20}.key-{index}")]);

    [Fact]
    public void The_generator_stays_linear_in_the_keys_it_is_given()
    {
        // A ratio is the right question here: the tree, the collision walk and the emitter each touch every
        // key, and the failure worth catching is one of them starting to touch every other key as well.
        var small = Allocation.PerCall(() => Generate(Vocabulary(100), Declared()), calls: 5);
        var large = Allocation.PerCall(() => Generate(Vocabulary(400), Declared()), calls: 5);

        var ratio = (double)large / small;

        Assert.True(
            ratio < _linearEnough,
            $"400 keys cost {ratio:F1}x what 100 did, expected under {_linearEnough}");
    }

    [Fact]
    public void The_family_rule_reads_its_declared_set_once_per_key_type()
    {
        // The regression this exists for: SL1012 resolved the key type's symbol and enumerated the whole
        // declared set once per authored key. Held to a figure rather than a ratio because doing that is
        // still linear in keys — it was four times steeper at four hundred keys and produced the same ratio.
        Under(_onePass,"SL1012 over 400 keys", () =>
            AnalyzeOn(_compilation, new VocabularyKeyFamilies(),
                [("Vocab.resx", Vocabulary(400), Declared("Probe.FiledKey"))]));
    }

    [Fact]
    public void The_heading_rule_reads_the_vocabulary_once()
    {
        Under(_onePass,"SL1011 over 400 keys", () =>
            AnalyzeOn(_compilation, new VocabularyHeadings(),
                [("Vocab.resx", Vocabulary(400), Declared())]));
    }

    private static void Under(long ceiling, string what, Action work)
    {
        var bytes = Allocation.Total(work);

        Assert.True(bytes <= ceiling, $"{what} allocated {bytes:N0} B, ceiling {ceiling:N0} B");
    }

    /// <summary>A set with enough members that walking it once per key would show.</summary>
    private static readonly Microsoft.CodeAnalysis.CSharp.CSharpCompilation _compilation = Empty($$"""
        using SimpleLocalizations;

        namespace Probe;

        [VocabularyFamily("section")]
        public enum Sections
        {
        {{string.Join(",\n", Enumerable.Range(0, 20).Select(index => $"    Section{index}"))}}
        }

        [VocabularyKey(Families = typeof(Sections))]
        public readonly struct FiledKey
        {
            private FiledKey(string key) => Key = key;
            public string Key { get; }
            public static FiledKey From(string key) => new(key);
        }
        """);
}
