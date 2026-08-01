using static SimpleLocalizations.Tests.VocabularyHarness;

namespace SimpleLocalizations.Tests;

/// <summary>
/// The body of a key type, which is generated because the shape <em>is</em> the rule: a hand-rolled type
/// that grew a public constructor would open the vocabulary again with nothing failing.
/// </summary>
public class KeyTypeGeneratorTests
{
    [Fact]
    public void A_marked_partial_struct_gets_a_private_constructor_and_one_factory()
    {
        var run = GenerateKeyTypes("""
            using SimpleLocalizations;

            namespace Probe;

            [VocabularyKey]
            public readonly partial struct FindingKey;
            """);

        Assert.Empty(run.Ids);
        Assert.Contains("namespace Probe;", run.OnlySource);
        Assert.Contains("public readonly partial struct FindingKey", run.OnlySource);
        Assert.Contains("private FindingKey(string key) => Key = key;", run.OnlySource);
        Assert.Contains("public string Key { get; }", run.OnlySource);
        Assert.Contains("public static FindingKey From(string key) => new(key);", run.OnlySource);
    }

    [Fact]
    public void A_record_struct_keeps_its_record_ness()
    {
        // The partial has to repeat the declaration exactly, or the two halves disagree and neither compiles.
        var run = GenerateKeyTypes("""
            using SimpleLocalizations;

            namespace Probe;

            [VocabularyKey]
            public readonly partial record struct FindingKey;
            """);

        Assert.Contains("public readonly partial record struct FindingKey", run.OnlySource);
    }

    [Fact]
    public void An_internal_key_type_stays_internal()
    {
        var run = GenerateKeyTypes("""
            using SimpleLocalizations;

            namespace Probe;

            [VocabularyKey]
            internal readonly partial struct FindingKey;
            """);

        Assert.Contains("internal readonly partial struct FindingKey", run.OnlySource);
    }

    [Fact]
    public void A_key_type_claiming_families_is_written_the_same_way()
    {
        // Families is a claim the rules read; it changes nothing about the body.
        var run = GenerateKeyTypes("""
            using SimpleLocalizations;

            namespace Probe;

            public enum Sections { Alpha }

            [VocabularyKey(Families = typeof(Sections))]
            public readonly partial struct FindingKey;
            """);

        Assert.Empty(run.Ids);
        Assert.Contains("public static FindingKey From(string key) => new(key);", run.OnlySource);
    }

    [Fact]
    public void SL1013_refuses_a_key_type_that_is_not_partial()
    {
        var run = GenerateKeyTypes("""
            using SimpleLocalizations;

            namespace Probe;

            [VocabularyKey]
            public readonly struct FindingKey;
            """);

        Assert.Equal(["SL1013"], run.Ids);
        Assert.Empty(run.Sources);
    }

    [Fact]
    public void A_nested_key_type_is_written_where_it_was_declared()
    {
        // Emitting the body at the namespace's own level declares a second, unrelated type: it compiles, and
        // the type that was declared keeps no body at all, so every call site fails naming neither.
        var run = GenerateKeyTypes("""
            using SimpleLocalizations;

            namespace Probe;

            public static partial class Vocabulary
            {
                [VocabularyKey]
                public readonly partial struct FindingKey;
            }
            """);

        Assert.Empty(run.Ids);
        Assert.Equal(["Probe.Vocabulary.FindingKey.g.cs"], run.Sources.Keys);
        Assert.Contains("public static partial class Vocabulary", run.OnlySource);
        Assert.Contains("    public readonly partial struct FindingKey", run.OnlySource);
        Assert.Contains("        public static FindingKey From(string key) => new(key);", run.OnlySource);
    }

    [Fact]
    public void A_nested_key_type_compiles_against_its_own_declaration()
    {
        // The emitted halves have to meet: same nesting, same modifiers, one type.
        var errors = Compile(
            [],
            """
            using SimpleLocalizations;

            namespace Probe;

            public static partial class Vocabulary
            {
                [VocabularyKey]
                public readonly partial record struct FindingKey;
            }

            public static class Caller
            {
                public static string Read(Vocabulary.FindingKey key) => key.Key;
            }
            """)
            .Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString());

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("public static class Vocabulary")]
    [InlineData("public partial class Vocabulary<T>")]
    public void SL1015_refuses_a_key_type_nested_in_a_type_that_cannot_be_reopened(string enclosing)
    {
        var run = GenerateKeyTypes($$"""
            using SimpleLocalizations;

            namespace Probe;

            {{enclosing}}
            {
                [VocabularyKey]
                public readonly partial struct FindingKey;
            }
            """);

        Assert.Equal(["SL1015"], run.Ids);
        Assert.Empty(run.Sources);
    }

    [Fact]
    public void A_type_carrying_no_marker_is_not_written()
    {
        var run = GenerateKeyTypes("namespace Probe { public readonly partial struct Plain { } }");

        Assert.Empty(run.Ids);
        Assert.Empty(run.Sources);
    }
}
