using SimpleLocalizations.Generator;
using static SimpleLocalizations.Tests.VocabularyHarness;

namespace SimpleLocalizations.Tests;

/// <summary>
/// The three rules that read symbols rather than a resource alone: the closed vocabulary, and the two that
/// tie a key's family to a set declared in code.
/// </summary>
public class VocabularyRuleTests
{
    private const string _categories = """
        namespace Probe;

        public enum Categories
        {
            Alpha,
            Beta,
        }
        """;

    private static Declaration Declared(string keyType = "Probe.ProbeKey", string headings = "") =>
        new("ProbeKeys", keyType, "Probe", Headings: headings);

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
    public void SL1011_refuses_a_declared_member_with_no_heading_authored()
    {
        var diagnostics = Analyze(
            new VocabularyHeadings(),
            _categories,
            [("Vocab.resx", Keys("heading.alpha"), Declared(headings: "heading=Probe.Categories"))]);

        Assert.Equal(["SL1011"], diagnostics.Select(d => d.Id));
        Assert.Contains("heading.beta", diagnostics[0].GetMessage());
    }

    [Fact]
    public void SL1011_is_satisfied_when_every_member_is_authored()
    {
        var ids = Analyze(
            new VocabularyHeadings(),
            _categories,
            [("Vocab.resx", Keys("heading.alpha", "heading.beta"), Declared(headings: "heading=Probe.Categories"))])
            .Select(d => d.Id);

        Assert.Empty(ids);
    }

    [Fact]
    public void SL1011_is_silent_where_nothing_declares_headings()
    {
        var ids = Analyze(
            new VocabularyHeadings(),
            _categories,
            [("Vocab.resx", Keys("heading.alpha"), Declared())])
            .Select(d => d.Id);

        Assert.Empty(ids);
    }

    [Fact]
    public void SL1012_refuses_a_key_filed_outside_the_declared_families()
    {
        var diagnostics = Analyze(
            new VocabularyKeyFamilies(),
            _categories,
            [("Vocab.resx", Keys("alpha.one", "gamma.two"), Declared())],
            keyFamilies: "Probe.ProbeKey=Probe.Categories");

        Assert.Equal(["SL1012"], diagnostics.Select(d => d.Id));
        Assert.Contains("gamma", diagnostics[0].GetMessage());
    }

    [Fact]
    public void SL1012_holds_only_the_key_type_it_names()
    {
        // A key of another type files nothing, so its family is nobody's claim.
        var ids = Analyze(
            new VocabularyKeyFamilies(),
            _categories,
            [("Vocab.resx", Keys("alpha.one", "gamma.two"), Declared("Probe.ProbeKey | gamma=Probe.OtherKey"))],
            keyFamilies: "Probe.ProbeKey=Probe.Categories")
            .Select(d => d.Id);

        Assert.Empty(ids);
    }

    [Fact]
    public void SL1012_is_silent_where_the_build_declares_no_families()
    {
        var ids = Analyze(
            new VocabularyKeyFamilies(),
            _categories,
            [("Vocab.resx", Keys("gamma.two"), Declared())])
            .Select(d => d.Id);

        Assert.Empty(ids);
    }

    [Fact]
    public void SL1012_is_silent_where_the_declared_set_is_not_on_this_compilation()
    {
        // A project may author keys of the type without referencing the assembly the families live in.
        var ids = Analyze(
            new VocabularyKeyFamilies(),
            "namespace Probe { public class Unrelated { } }",
            [("Vocab.resx", Keys("gamma.two"), Declared())],
            keyFamilies: "Probe.ProbeKey=Probe.Missing")
            .Select(d => d.Id);

        Assert.Empty(ids);
    }
}
