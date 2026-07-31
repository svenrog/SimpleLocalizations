using SimpleLocalizations.Generator;
using static SimpleLocalizations.Tests.VocabularyHarness;

namespace SimpleLocalizations.Tests;

/// <summary>
/// The three rules that read symbols rather than a resource alone: the closed vocabulary, and the two that
/// tie a key's family to a set declared in code.
/// </summary>
public class VocabularyRuleTests
{
    /// <summary>A set whose members are headings, and a key type filed under it.</summary>
    private const string _declared = """
        using SimpleLocalizations;

        namespace Probe;

        [VocabularyFamily("heading")]
        public enum Sections
        {
            Alpha,
            Beta,
        }

        [VocabularyKey(Families = typeof(Sections))]
        public readonly struct FiledKey
        {
            private FiledKey(string key) => Key = key;
            public string Key { get; }
            public static FiledKey From(string key) => new(key);
        }

        [VocabularyKey]
        public readonly struct FreeKey
        {
            private FreeKey(string key) => Key = key;
            public string Key { get; }
            public static FreeKey From(string key) => new(key);
        }
        """;

    private static Declaration Declared(string keyType = "Probe.FiledKey") =>
        new("ProbeKeys", keyType, "Probe");

    [Fact]
    public void SL1010_refuses_a_key_produced_at_a_call_site()
    {
        var ids = Analyze(new VocabularyKeyGuard(), $$"""
            {{KeyTypes}}

            public static class Caller
            {
                public static Probe.ProbeKey Invented() => Probe.ProbeKey.From("alpha.one");
            }
            """).Select(d => d.Id);

        Assert.Equal(["SL1010"], ids);
    }

    [Fact]
    public void SL1010_refuses_the_factory_handed_on_as_a_method_group()
    {
        // A delegate hands the ability to invent a key to whatever holds it, so a bare method group counts.
        var ids = Analyze(new VocabularyKeyGuard(), $$"""
            using System;

            {{KeyTypes}}

            public static class Caller
            {
                public static Func<string, Probe.ProbeKey> Held => Probe.ProbeKey.From;
            }
            """).Select(d => d.Id);

        Assert.Equal(["SL1010"], ids);
    }

    [Fact]
    public void SL1010_leaves_a_type_carrying_no_marker_alone()
    {
        var ids = Analyze(new VocabularyKeyGuard(), """
            namespace Probe;

            public readonly struct Plain
            {
                public static Plain From(string key) => new();
            }

            public static class Caller
            {
                public static Plain Made() => Plain.From("anything");
            }
            """).Select(d => d.Id);

        Assert.Empty(ids);
    }

    [Fact]
    public void SL1011_refuses_a_declared_member_with_no_key_authored()
    {
        var diagnostics = Analyze(
            new VocabularyHeadings(),
            _declared,
            [("Vocab.resx", Keys("heading.alpha"), Declared())]);

        Assert.Equal(["SL1011"], diagnostics.Select(d => d.Id));
        Assert.Contains("heading.beta", diagnostics[0].GetMessage());
    }

    [Fact]
    public void SL1011_is_satisfied_when_every_member_is_authored()
    {
        var ids = Analyze(
            new VocabularyHeadings(),
            _declared,
            [("Vocab.resx", Keys("heading.alpha", "heading.beta"), Declared())])
            .Select(d => d.Id);

        Assert.Empty(ids);
    }

    [Fact]
    public void SL1011_is_silent_where_no_set_declares_a_family()
    {
        var ids = Analyze(
            new VocabularyHeadings(),
            "namespace Probe { public enum Sections { Alpha } }",
            [("Vocab.resx", Keys("heading.alpha"), Declared())])
            .Select(d => d.Id);

        Assert.Empty(ids);
    }

    [Fact]
    public void SL1012_refuses_a_key_filed_outside_the_declared_families()
    {
        var diagnostics = Analyze(
            new VocabularyKeyFamilies(),
            _declared,
            [("Vocab.resx", Keys("alpha.one", "gamma.two"), Declared())]);

        Assert.Equal(["SL1012"], diagnostics.Select(d => d.Id));
        Assert.Contains("gamma", diagnostics[0].GetMessage());
    }

    [Fact]
    public void SL1012_holds_only_a_key_type_that_claims_families()
    {
        // A key type claiming none files nothing, so its family is nobody's claim.
        var ids = Analyze(
            new VocabularyKeyFamilies(),
            _declared,
            [("Vocab.resx", Keys("gamma.two"), Declared("Probe.FreeKey"))])
            .Select(d => d.Id);

        Assert.Empty(ids);
    }

    [Fact]
    public void SL1012_holds_each_family_to_the_type_declared_for_it()
    {
        // The per-family override decides which claim applies, so one resource can hold both kinds.
        var ids = Analyze(
            new VocabularyKeyFamilies(),
            _declared,
            [("Vocab.resx", Keys("alpha.one", "gamma.two"), Declared("Probe.FiledKey | gamma=Probe.FreeKey"))])
            .Select(d => d.Id);

        Assert.Empty(ids);
    }

    [Fact]
    public void SL1012_is_silent_where_the_key_type_is_not_on_this_compilation()
    {
        // A project may author keys of a type it cannot see the declaration of.
        var ids = Analyze(
            new VocabularyKeyFamilies(),
            "namespace Probe { public class Unrelated { } }",
            [("Vocab.resx", Keys("gamma.two"), Declared("Probe.Missing"))])
            .Select(d => d.Id);

        Assert.Empty(ids);
    }
}
