using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SimpleLocalizations.Generator;
using static SimpleLocalizations.Tests.VocabularyHarness;

namespace SimpleLocalizations.Tests.Performance;

/// <summary>
/// What a second run costs. This is the generator's whole performance story: a build runs it once, but an
/// IDE runs it on every keystroke, and the difference between reusing a stage and repeating it is the
/// difference between a component nobody notices and one that makes typing stutter.
/// <para>
/// Asserted on the reasons Roslyn records rather than on a clock, so the answer is the same on any machine
/// and can be allowed to fail a build. A model that stopped comparing by value — an
/// <see cref="AdditionalText"/> held instead of its text, a collection where a string was — would show up
/// here as <c>Modified</c> and nowhere else until someone complained about their editor.
/// </para>
/// </summary>
public class IncrementalCachingTests
{
    private static readonly IReadOnlyList<(string, string, Declaration)> _resources =
    [
        ("Vocab.resx",
            Keys([.. Enumerable.Range(0, 200).Select(index => $"family{index % 10}.key-{index}")]),
            new Declaration("ProbeKeys", "SimpleLocalizations.LocalizationKey", "Probe")),
    ];

    private const string _keyType = """
        using SimpleLocalizations;

        namespace Probe;

        [VocabularyKey]
        public readonly partial record struct FindingKey;
        """;

    [Theory]
    [InlineData(VocabularyGenerator.Stages.Described)]
    [InlineData(VocabularyGenerator.Stages.Vocabularies)]
    public void A_vocabulary_is_not_re_read_when_something_else_changes(string stage)
    {
        var driver = Tracking(_resources, new VocabularyGenerator());
        var compilation = Empty();

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        driver = driver.RunGenerators(Edited(compilation), TestContext.Current.CancellationToken);

        Assert.All(Reasons(driver, stage), reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));
    }

    [Fact]
    public void No_source_is_re_emitted_when_something_else_changes()
    {
        // The expensive end of the pipeline. A stage that recomputes is wasted work; a source output that
        // re-emits also invalidates everything the compiler had already bound against it.
        var driver = Tracking(_resources, new VocabularyGenerator());
        var compilation = Empty();

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        driver = driver.RunGenerators(Edited(compilation), TestContext.Current.CancellationToken);

        Assert.All(
            Reasons(driver, "SourceOutput"),
            reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));
    }

    [Theory]
    [InlineData(KeyTypeGenerator.Stages.Described)]
    [InlineData(KeyTypeGenerator.Stages.KeyTypes)]
    public void A_key_type_is_not_re_described_when_an_unrelated_type_changes(string stage)
    {
        var driver = Tracking([], new KeyTypeGenerator());
        var compilation = Empty(_keyType);

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        driver = driver.RunGenerators(Edited(compilation), TestContext.Current.CancellationToken);

        Assert.All(
            Reasons(driver, stage),
            reason => Assert.Contains(
                reason, new[] { IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged }));
    }

    [Fact]
    public void An_edited_resource_is_re_read_and_re_emitted()
    {
        // The other half of the claim: caching that never invalidates is not caching, it is a stale build.
        var driver = Tracking(_resources, new VocabularyGenerator());
        driver = driver.RunGenerators(Empty(), TestContext.Current.CancellationToken);

        var edited = Tracking(
            [("Vocab.resx", Keys("alpha.one", "alpha.two"), _resources[0].Item3)],
            new VocabularyGenerator());

        edited = edited.RunGenerators(Empty(), TestContext.Current.CancellationToken);

        Assert.Contains("Two =>", edited.GetRunResult().Results
            .SelectMany(result => result.GeneratedSources)
            .Single()
            .SourceText.ToString());
    }

    /// <summary>A change the generator does not read: another file, in a compilation it never inspects.</summary>
    private static CSharpCompilation Edited(CSharpCompilation compilation) =>
        compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText(
                "internal sealed class Unrelated { public int Value { get; set; } }",
                cancellationToken: TestContext.Current.CancellationToken));
}
