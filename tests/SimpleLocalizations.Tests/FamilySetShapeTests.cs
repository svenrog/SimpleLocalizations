using SimpleLocalizations.Generator;
using static SimpleLocalizations.Tests.VocabularyHarness;

namespace SimpleLocalizations.Tests;

/// <summary>
/// The shapes a closed set is written in. All of them are one claim — "these are the members" — so all of
/// them are read, and a set whose shape is not one of them says so rather than checking nothing.
/// </summary>
public class FamilySetShapeTests
{
    private static Declaration Declared() => new("ProbeKeys", "Probe.FreeKey", "Probe");

    private static IEnumerable<string> Check(string set, params string[] authored) =>
        Analyze(
            new VocabularyHeadings(),
            $$"""
            using SimpleLocalizations;

            namespace Probe;

            {{set}}

            [VocabularyKey]
            public readonly struct FreeKey
            {
                private FreeKey(string key) => Key = key;
                public string Key { get; }
                public static FreeKey From(string key) => new(key);
            }
            """,
            [("Vocab.resx", authored.Length == 0 ? Keys("unrelated.key") : Keys(authored), Declared())])
        .Select(diagnostic => diagnostic.GetMessage());

    [Fact]
    public void An_enums_members_are_its_member_names()
    {
        Assert.Empty(Check(
            """
            [VocabularyFamily("heading")]
            public enum Sections { Alpha, Beta }
            """,
            "heading.alpha", "heading.beta"));
    }

    [Fact]
    public void A_constant_is_spelled_by_its_value_rather_than_its_name()
    {
        // A value is why it is a constant and not an enum, so the value is the identity.
        Assert.Empty(Check(
            """
            [VocabularyFamily("heading")]
            public static class Sections
            {
                public const string Alpha = "alpha";
                public const string SecurityTxt = "security-txt";
            }
            """,
            "heading.alpha", "heading.security-txt"));
    }

    [Fact]
    public void A_constant_whose_value_is_not_a_string_falls_back_to_its_name()
    {
        Assert.Empty(Check(
            """
            [VocabularyFamily("heading")]
            public static class Sections
            {
                public const int Alpha = 1;
            }
            """,
            "heading.alpha"));
    }

    [Fact]
    public void Static_readonly_instances_are_members()
    {
        // The type-safe enum a class reaches for when a member needs behaviour: no value to read, so the
        // member name spells it.
        Assert.Empty(Check(
            """
            [VocabularyFamily("heading")]
            public sealed class Sections
            {
                private Sections(int rank) => Rank = rank;

                public int Rank { get; }

                public static readonly Sections Alpha = new(1);
                public static readonly Sections Beta = new(2);
            }
            """,
            "heading.alpha", "heading.beta"));
    }

    [Fact]
    public void Static_properties_of_the_declaring_type_are_members()
    {
        Assert.Empty(Check(
            """
            [VocabularyFamily("heading")]
            public sealed class Sections
            {
                private Sections() { }

                public static Sections Alpha { get; } = new();
                public static Sections Beta { get; } = new();
            }
            """,
            "heading.alpha", "heading.beta"));
    }

    [Fact]
    public void A_subclassed_member_still_belongs_to_the_set_it_derives_from()
    {
        Assert.Empty(Check(
            """
            [VocabularyFamily("heading")]
            public abstract class Sections
            {
                private sealed class Loud : Sections { }

                public static readonly Sections Alpha = new Loud();
            }
            """,
            "heading.alpha"));
    }

    [Fact]
    public void A_static_member_of_an_unrelated_type_is_not_a_member()
    {
        // Only the set's own instances count, or every helper on the type would demand a key.
        Assert.Empty(Check(
            """
            [VocabularyFamily("heading")]
            public sealed class Sections
            {
                private Sections() { }

                public static Sections Alpha { get; } = new();
                public static string Helper { get; } = "not a member";
                public static readonly object Other = new();
            }
            """,
            "heading.alpha"));
    }

    [Fact]
    public void A_missing_member_is_reported_whatever_shape_the_set_takes()
    {
        var messages = Check(
            """
            [VocabularyFamily("heading")]
            public sealed class Sections
            {
                private Sections() { }

                public static readonly Sections Alpha = new();
                public static readonly Sections Beta = new();
            }
            """,
            "heading.alpha").ToList();

        Assert.Single(messages);
        Assert.Contains("heading.beta", messages[0]);
    }

    [Fact]
    public void SL1014_refuses_a_set_nothing_can_enumerate()
    {
        // Checking nothing and saying nothing reads exactly like a set whose every member is authored.
        var ids = Analyze(
            new VocabularyHeadings(),
            """
            using SimpleLocalizations;

            namespace Probe;

            [VocabularyFamily("heading")]
            public sealed class Sections
            {
                public string Name { get; set; } = "";
            }
            """,
            [("Vocab.resx", Keys("heading.alpha"), Declared())])
            .Select(diagnostic => diagnostic.Id);

        Assert.Equal(["SL1014"], ids);
    }
}
