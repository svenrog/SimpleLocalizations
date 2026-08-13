using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static SimpleLocalizations.Tests.VocabularyHarness;

namespace SimpleLocalizations.Tests;

/// <summary>
/// The call sites `README.md` and `docs/` show, compiled — read out of the markdown itself, so an example
/// edited in place is the one that runs. Documentation is the one place a rename cannot reach: a key type
/// renamed or an overload dropped leaves the prose reading exactly as it did.
/// </summary>
public class DocumentedExamplesTests
{
    /// <summary>What a marked block claims about itself: a call site, or generated source shown as such.</summary>
    private const string _compiles = "compiles";

    private const string _illustrative = "illustrative";

    /// <summary>
    /// The resources a block is compiled against, and the declarations it is written as if it had. Named by
    /// the marker's argument: <c>&lt;!-- compiles: strings --&gt;</c> above a fence picks this one.
    /// </summary>
    private sealed record Fixture(
        IReadOnlyList<(string FileName, string Resx, Declaration Declaration)> Resources, string Preamble = "");

    private static readonly Dictionary<string, Fixture> _fixtures = new(StringComparer.Ordinal)
    {
        ["strings"] = new(
            [("Strings.resx",
                Resx(("greeting", "Hello, {0}", "Becomes the generated member's XmlDoc.")),
                new Declaration(
                    "StringsKeys", "SimpleLocalizations.LocalizationKey", "Probe", ResourceName: "Probe.Strings"))]),
        ["keytype"] = new(
            [("SecurityStrings.resx", Keys("cookies.insecure"),
                new Declaration(
                    "SecurityKeys", "Probe.FindingKey", "Probe", ResourceName: "Probe.SecurityStrings"))]),
        ["security"] = new(
            [("SecurityStrings.resx", Keys("cookies.insecure"),
                new Declaration(
                    "SecurityKeys", "Probe.FindingKey", "Probe", ResourceName: "Probe.SecurityStrings"))],
            """
            namespace Probe
            {
                [VocabularyKey]
                public readonly partial record struct FindingKey;
            }
            """),
        ["lookup"] = new(
            [("SecurityStrings.resx", Keys("category.company", "category.stack", "category.tls"),
                new Declaration(
                    "SecurityKeys", "SimpleLocalizations.LocalizationKey", "Probe",
                    ResourceName: "Probe.SecurityStrings"))],
            """
            namespace Probe
            {
                [VocabularyFamily("category")]
                public enum FindingCategories { Tls, Stack, Company }
            }
            """),
        // No resource: list patterns are not lowercase and so are not a vocabulary. The marker type stands in
        // for whatever the consumer's assembly happens to hold.
        ["patterns"] = new(
            [],
            """
            namespace Probe
            {
                public static class ListPatterns;
            }
            """),
        ["console"] = new(
            [("Console.resx", Keys("greeting"),
                new Declaration(
                    "ConsoleKeys", "SimpleLocalizations.LocalizationKey", "Probe", ResourceName: "Probe.Console"))]),
    };

    public static TheoryData<string, string, string> Documented()
    {
        var data = new TheoryData<string, string, string>();

        foreach (var block in DocumentedCode.Blocks("csharp").Where(block => block.Kind == _compiles))
        {
            data.Add(block.ToString(), block.Argument, block.Code);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Documented))]
    public void A_documented_example_compiles(string source, string fixture, string code)
    {
        Assert.True(_fixtures.ContainsKey(fixture), $"{source} names no fixture '{fixture}'");

        var errors = Errors(_fixtures[fixture], code);

        Assert.True(errors.Count == 0, $"{source}\n{string.Join("\n", errors)}");
    }

    [Fact]
    public void Every_documented_csharp_block_says_whether_it_compiles()
    {
        // A block added without a marker would be documentation nothing reads back, which is what this whole
        // file exists to prevent — so an unmarked one fails here rather than going quietly untested.
        var unaccounted = DocumentedCode.Blocks("csharp")
            .Where(block => block.Kind is not (_compiles or _illustrative))
            .Select(block => block.ToString());

        Assert.Empty(unaccounted);
    }

    [Fact]
    public void A_declared_key_type_is_spelled_by_its_Key_because_the_catalog_takes_no_other()
    {
        // The reason the typed-key examples end in `.Key` and the default-typed ones do not. Written out
        // rather than read from the docs, because it is the example the docs do *not* show.
        var errors = Errors(_fixtures["security"], "var shown = catalog.Get(SecurityKeys.Cookies.Insecure);");

        Assert.NotEmpty(errors);
    }

    private static IReadOnlyList<string> Errors(Fixture fixture, string code) =>
        [.. Compile(fixture.Resources, Scaffold(code, fixture.Preamble))
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())];

    /// <summary>
    /// A block as a compilation. One declaring types is one already; anything else is statements, which get a
    /// method to sit in and the <c>catalog</c> the prose around them has. Asked of the parser rather than of
    /// the text, so a block that opens with a comment or an attribute is read the same way the compiler does.
    /// </summary>
    private static string Scaffold(string code, string preamble) =>
        SyntaxFactory.ParseCompilationUnit(code).Members.Any(member => member is BaseTypeDeclarationSyntax)
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
