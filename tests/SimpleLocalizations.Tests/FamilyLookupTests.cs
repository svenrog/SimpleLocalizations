using Microsoft.CodeAnalysis;
using SimpleLocalizations.Generator;
using static SimpleLocalizations.Tests.VocabularyHarness;

namespace SimpleLocalizations.Tests;

/// <summary>
/// The lookup from a declared set's member to the key wording it. The words a set owes are already the
/// analyzer's subject (<c>SL1011</c>), so the mapping is one the build knows both ends of — and a consumer
/// hand-writing it writes a second copy of how a member is spelled, which the compiler cannot check against
/// the first.
/// </summary>
public class FamilyLookupTests
{
    private const string _set = """
        using SimpleLocalizations;

        namespace Probe;

        [VocabularyFamily("decline-reason")]
        public enum DeclineReason { Blocked, NotApplicable }
        """;

    private static Declaration Declared(string keyType = "Probe.ProbeKey") =>
        new("ProbeKeys", keyType, "Probe");

    private static string Emitted(string set = _set, params string[] authored) =>
        Generate(Keys(authored), Declared(), set).OnlySource;

    private static int Occurrences(string source, string fragment) =>
        (source.Length - source.Replace(fragment, "").Length) / fragment.Length;

    [Fact]
    public void A_declared_enum_is_looked_up_by_the_family_it_words()
    {
        var source = Emitted(_set, "decline-reason.blocked", "decline-reason.not-applicable");

        Assert.Contains("public static global::Probe.ProbeKey Of(global::Probe.DeclineReason member) =>", source);
        Assert.Contains("global::Probe.DeclineReason.Blocked => Blocked,", source);
        Assert.Contains("global::Probe.DeclineReason.NotApplicable => NotApplicable,", source);
    }

    [Fact]
    public void The_lookup_produces_the_key_type_its_family_does()
    {
        // A family declaring a type of its own is what the lookup returns, or it is the one member of the
        // family that answers with a key of the wrong kind.
        var source = Generate(
            Keys("decline-reason.blocked"),
            new Declaration("ProbeKeys", "Probe.ProbeKey | decline-reason=Probe.OtherKey", "Probe"),
            _set)
            .OnlySource;

        Assert.Contains("public static global::Probe.OtherKey Of(", source);
    }

    [Fact]
    public void A_member_with_no_key_authored_takes_no_arm()
    {
        // It cannot: there is no member to point at. SL1011 is what names the absent word; the arm that
        // would have taken it throws, so the silence reaches a caller rather than a wrong key.
        var source = Emitted(_set, "decline-reason.blocked");

        Assert.Contains("global::Probe.DeclineReason.Blocked => Blocked,", source);
        Assert.DoesNotContain("NotApplicable =>", source);
        Assert.Contains("_ => throw new global::System.ArgumentOutOfRangeException(nameof(member)),", source);
    }

    [Fact]
    public void A_set_nothing_outside_the_assembly_can_name_gets_a_lookup_nothing_outside_it_can_call()
    {
        // A generated class is public, and a public method taking an internal parameter does not compile.
        var source = Emitted(
            """
            using SimpleLocalizations;

            namespace Probe;

            [VocabularyFamily("decline-reason")]
            internal enum DeclineReason { Blocked }
            """,
            "decline-reason.blocked");

        Assert.Contains("internal static global::Probe.ProbeKey Of(", source);
    }

    [Fact]
    public void A_set_that_is_not_an_enum_is_not_looked_up()
    {
        // The lookup is a switch over the set. A constant is a value a caller already holds the spelling of,
        // and a static readonly instance has no case label.
        var source = Emitted(
            """
            using SimpleLocalizations;

            namespace Probe;

            [VocabularyFamily("decline-reason")]
            public static class DeclineReasons
            {
                public const string Blocked = "blocked";
            }
            """,
            "decline-reason.blocked");

        Assert.DoesNotContain(" Of(", source);
    }

    [Fact]
    public void A_set_worded_under_several_families_is_looked_up_in_each()
    {
        // One set, worded once per thing it grades. The alternative is a switch per family, which is the
        // copy this generates away — and only one of them could be held to its words.
        var source = Emitted(
            """
            using SimpleLocalizations;

            namespace Probe;

            [VocabularyFamily("decline-reason")]
            [VocabularyFamily("other-reason")]
            public enum DeclineReason { Blocked }
            """,
            "decline-reason.blocked", "other-reason.blocked");

        Assert.Contains("Of(global::Probe.DeclineReason member)", source);
        Assert.Equal(2, Occurrences(source, "global::Probe.DeclineReason.Blocked => Blocked,"));
    }

    [Fact]
    public void A_family_two_sets_word_is_looked_up_by_neither()
    {
        var both = """
            using SimpleLocalizations;

            namespace Probe;

            [VocabularyFamily("decline-reason")]
            public enum DeclineReason { Blocked }

            [VocabularyFamily("decline-reason")]
            public enum OtherReason { Blocked }
            """;

        Assert.DoesNotContain(" Of(", Emitted(both, "decline-reason.blocked"));
    }

    [Fact]
    public void SL1011_holds_a_set_to_the_words_of_every_family_it_words()
    {
        var reported = Analyze(
            new VocabularyHeadings(),
            """
            using SimpleLocalizations;

            namespace Probe;

            [VocabularyFamily("decline-reason")]
            [VocabularyFamily("other-reason")]
            public enum DeclineReason { Blocked }
            """,
            [("Vocab.resx", Keys("decline-reason.blocked"), Declared())])
            .Select(diagnostic => diagnostic.GetMessage());

        Assert.Contains("other-reason.blocked", Assert.Single(reported));
    }

    [Fact]
    public void SL1016_names_the_pair_that_claimed_one_family()
    {
        var reported = Analyze(
            new VocabularyHeadings(),
            """
            using SimpleLocalizations;

            namespace Probe;

            [VocabularyFamily("decline-reason")]
            public enum DeclineReason { Blocked }

            [VocabularyFamily("decline-reason")]
            public enum OtherReason { Blocked }
            """,
            [("Vocab.resx", Keys("decline-reason.blocked"), Declared())]);

        var contested = Assert.Single(reported.Where(diagnostic => diagnostic.Id == "SL1016"));

        Assert.Contains("OtherReason", contested.GetMessage());
        Assert.Contains("DeclineReason", contested.GetMessage());
    }

    [Fact]
    public void A_set_whose_family_this_resource_does_not_author_is_not_looked_up()
    {
        Assert.DoesNotContain(" Of(", Emitted(_set, "unrelated.key"));
    }

    [Fact]
    public void A_key_worded_of_does_not_take_the_lookup_s_name()
    {
        // Of joins Prefix and Covers as a name the family carries itself, so a segment spelled that way is
        // emitted as OfKey — the rule that already keeps a key named 'prefix' from colliding.
        var source = Emitted(_set, "decline-reason.blocked", "decline-reason.of");

        Assert.Contains("OfKey =>", source);
    }

    [Fact]
    public void A_lookup_and_its_call_site_compile()
    {
        // The substring assertions above cannot see a signature that does not bind: the arms name members of
        // the class the lookup is written into, and the class is named after the family rather than the set.
        var errors = Compile(
            [("Vocab.resx", Keys("decline-reason.blocked", "decline-reason.not-applicable"), Declared())],
            $$"""
            {{_set}}

            [VocabularyKey]
            public readonly partial struct ProbeKey;

            internal static class Caller
            {
                public static string Named() => ProbeKeys.DeclineReason.Of(DeclineReason.NotApplicable).Key;
            }
            """)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString());

        Assert.Empty(errors);
    }
}
