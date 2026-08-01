using Microsoft.CodeAnalysis;
using static SimpleLocalizations.Tests.VocabularyHarness;

namespace SimpleLocalizations.Tests;

/// <summary>
/// The call sites `README.md` and `docs/` show, compiled. Documentation is the one place a rename cannot
/// reach — a key type renamed or an overload dropped leaves the prose reading exactly as it did — so the
/// examples are held here rather than trusted.
/// </summary>
public class DocumentedExamplesTests
{
    private static IEnumerable<string> Errors(
        IReadOnlyList<(string FileName, string Resx, Declaration Declaration)> resources, string source) =>
        Compile(resources, source)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString());

    [Fact]
    public void The_readme_simple_path_compiles()
    {
        var errors = Errors(
            [("Strings.resx", Resx(("greeting", "Hello, {0}", "Becomes the generated member's XmlDoc.")),
                new Declaration(
                    Class: "StringsKeys",
                    KeyType: "SimpleLocalizations.LocalizationKey",
                    Namespace: "Probe",
                    ResourceName: "Probe.Strings"))],
            """
            using Probe;
            using SimpleLocalizations;

            internal static class Readme
            {
                public static string Run()
                {
                    var text = StringsKeys.Catalog(new TextCultures("en-US", "sv-SE"));

                    text.Format(StringsKeys.Greeting, "world");
                    return text.Neutral(StringsKeys.Greeting, "world");
                }
            }
            """);

        Assert.Empty(errors);
    }

    [Fact]
    public void The_typed_key_example_compiles()
    {
        var errors = Errors(
            [("SecurityStrings.resx", Keys("cookies.insecure"),
                new Declaration(
                    Class: "SecurityKeys",
                    KeyType: "Probe.FindingKey",
                    Namespace: "Probe",
                    ResourceName: "Probe.SecurityStrings"))],
            """
            using Probe;
            using SimpleLocalizations;

            namespace Probe
            {
                [VocabularyKey]
                public readonly partial record struct FindingKey;
            }

            internal static class Docs
            {
                public static string Run(StringCatalog catalog)
                {
                    var stored = catalog.Neutral(SecurityKeys.Cookies.Insecure.Key);
                    var shown = catalog.Get(SecurityKeys.Cookies.Insecure.Key);

                    return SecurityKeys.Cookies.Covers(SecurityKeys.Cookies.Insecure.Key)
                        ? stored + shown + SecurityKeys.Cookies.Prefix + SecurityKeys.ResourceName
                        : "";
                }
            }
            """);

        Assert.Empty(errors);
    }

    [Fact]
    public void A_declared_key_type_is_spelled_by_its_Key_because_the_catalog_takes_no_other()
    {
        // Why the typed-key examples end in `.Key` and the default-typed ones do not: the catalog overloads a
        // string and the shipped LocalizationKey, and a third overload per declared type is exactly the
        // substitution a declared type exists to refuse.
        var errors = Errors(
            [("SecurityStrings.resx", Keys("cookies.insecure"),
                new Declaration("SecurityKeys", "Probe.FindingKey", "Probe", ResourceName: "Probe.SecurityStrings"))],
            """
            using Probe;
            using SimpleLocalizations;

            namespace Probe
            {
                [VocabularyKey]
                public readonly partial record struct FindingKey;
            }

            internal static class Docs
            {
                public static string Run(StringCatalog catalog) => catalog.Get(SecurityKeys.Cookies.Insecure);
            }
            """);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void The_flat_example_passes_a_default_typed_key_whole()
    {
        var errors = Errors(
            [("Console.resx", Keys("greeting"),
                new Declaration(
                    Class: "ConsoleKeys",
                    KeyType: "SimpleLocalizations.LocalizationKey",
                    Namespace: "Probe",
                    ResourceName: "Probe.Console"))],
            """
            using Probe;
            using SimpleLocalizations;

            internal static class Docs
            {
                public static string Run(StringCatalog catalog) => catalog.Get(ConsoleKeys.Greeting);
            }
            """);

        Assert.Empty(errors);
    }
}
