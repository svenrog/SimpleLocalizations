using Microsoft.CodeAnalysis;
using static SimpleLocalizations.Tests.VocabularyHarness;

namespace SimpleLocalizations.Tests;

/// <summary>
/// The call sites `README.md` and `docs/` show, compiled — read out of the markdown itself, so an example
/// edited in place is the one that runs. Documentation is the one place a rename cannot reach: a key type
/// renamed or an overload dropped leaves the prose reading exactly as it did.
/// </summary>
public class DocumentedExamplesTests
{
    /// <summary>
    /// The resources a marked block is compiled against, and the declarations it is written as if it had.
    /// Named by the marker: <c>&lt;!-- compiles: strings --&gt;</c> above a fence picks this one.
    /// </summary>
    private static readonly Dictionary<string, (
        IReadOnlyList<(string FileName, string Resx, Declaration Declaration)> Resources, string Preamble)>
        _fixtures = new(StringComparer.Ordinal)
        {
            ["strings"] = (
                [("Strings.resx",
                    Resx(("greeting", "Hello, {0}", "Becomes the generated member's XmlDoc.")),
                    new Declaration("StringsKeys", "SimpleLocalizations.LocalizationKey", "Probe", ResourceName: "Probe.Strings"))],
                ""),
            ["keytype"] = (
                [("SecurityStrings.resx", Keys("cookies.insecure"),
                    new Declaration("SecurityKeys", "Probe.FindingKey", "Probe", ResourceName: "Probe.SecurityStrings"))],
                ""),
            ["security"] = (
                [("SecurityStrings.resx", Keys("cookies.insecure"),
                    new Declaration("SecurityKeys", "Probe.FindingKey", "Probe", ResourceName: "Probe.SecurityStrings"))],
                """
                namespace Probe
                {
                    [VocabularyKey]
                    public readonly partial record struct FindingKey;
                }
                """),
            ["console"] = (
                [("Console.resx", Keys("greeting"),
                    new Declaration("ConsoleKeys", "SimpleLocalizations.LocalizationKey", "Probe", ResourceName: "Probe.Console"))],
                ""),
        };

    public static TheoryData<string, string, string> Documented()
    {
        var data = new TheoryData<string, string, string>();

        foreach (var block in DocumentedCode.Blocks().Where(block => block.Compiles))
        {
            data.Add(block.Source, block.Fixture, block.Code);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Documented))]
    public void A_documented_example_compiles(string source, string fixture, string code)
    {
        Assert.True(_fixtures.ContainsKey(fixture), $"{source} names no fixture '{fixture}'");

        var (resources, preamble) = _fixtures[fixture];

        var errors = Compile(resources, Scaffold(code, preamble))
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .ToList();

        Assert.True(errors.Count == 0, $"{source}:\n{string.Join("\n", errors)}");
    }

    [Fact]
    public void Every_documented_csharp_block_says_whether_it_compiles()
    {
        // A block added without a marker would be documentation nothing reads back, which is what this whole
        // file exists to prevent — so an unmarked one fails here rather than going quietly untested.
        Assert.Empty(DocumentedCode.Unmarked());
    }

    [Fact]
    public void A_declared_key_type_is_spelled_by_its_Key_because_the_catalog_takes_no_other()
    {
        // The reason the typed-key examples end in `.Key` and the default-typed ones do not. Written out
        // rather than read from the docs, because it is the example the docs do *not* show.
        var (resources, preamble) = _fixtures["security"];

        var errors = Compile(resources, Scaffold("var shown = catalog.Get(SecurityKeys.Cookies.Insecure);", preamble))
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        Assert.NotEmpty(errors);
    }

    /// <summary>
    /// A block as a compilation. A block declaring types is one already; anything else is statements, which
    /// get a method to sit in and the <c>catalog</c> the prose around them has.
    /// </summary>
    private static string Scaffold(string code, string preamble) =>
        code.TrimStart().StartsWith("[", StringComparison.Ordinal)
            ? $"""
                using SimpleLocalizations;

                namespace Probe;

                {code}
                """
            : $$"""
                using Probe;
                using SimpleLocalizations;

                {{preamble}}

                internal static class Example
                {
                    public static void Run(SimpleLocalizations.StringCatalog catalog)
                    {
                {{code}}
                    }
                }
                """;
}
