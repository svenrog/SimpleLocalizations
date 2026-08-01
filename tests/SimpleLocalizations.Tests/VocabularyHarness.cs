using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SimpleLocalizations.Generator;
using System.Collections.Immutable;

namespace SimpleLocalizations.Tests;

/// <summary>
/// Drives the component the way a build does: a compilation, resource files as <c>AdditionalFiles</c>, and
/// the declaration as analyzer-config metadata. Nothing here stubs the parse — a value reaches the generator
/// through the same path a csproj sends it down.
/// </summary>
internal static class VocabularyHarness
{
    /// <summary>The declaration a resource carries, as the metadata names the build writes.</summary>
    internal sealed record Declaration(
        string Class,
        string KeyType,
        string Namespace,
        string Derived = "",
        string ResourceName = "",
        string Hint = "");

    /// <summary>What one run produced: every diagnostic, and every file the generator emitted.</summary>
    internal sealed record Run(
        ImmutableArray<Diagnostic> Diagnostics,
        IReadOnlyDictionary<string, string> Sources)
    {
        public IEnumerable<string> Ids => Diagnostics.Select(d => d.Id);

        public string OnlySource => Sources.Values.Single();
    }

    /// <summary>Runs the generator over one resource.</summary>
    public static Run Generate(string resx, Declaration declaration, string source = "") =>
        Generate([(FileName: "Vocab.resx", Resx: resx, Declaration: declaration)], source);

    /// <summary>Runs the generator over several resources, which is how a real project declares more than one.</summary>
    public static Run Generate(
        IReadOnlyList<(string FileName, string Resx, Declaration Declaration)> resources,
        string source = "")
    {
        var compilation = Compile(source);
        var files = Files(resources);

        var driver = CSharpGeneratorDriver
            .Create([new VocabularyGenerator().AsSourceGenerator()], files.Texts, optionsProvider: files.Options)
            .RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var diagnostics);

        var sources = driver.GetRunResult().Results
            .SelectMany(result => result.GeneratedSources)
            .ToDictionary(generated => generated.HintName, generated => generated.SourceText.ToString());

        _ = updated;
        return new Run(diagnostics, sources);
    }

    /// <summary>
    /// Both generators over <paramref name="resources"/>, then the C# compiler over everything they produced
    /// together with <paramref name="source"/> — which is what a documented call site is: generated members
    /// and the runtime's own overloads, resolved against each other.
    /// </summary>
    public static ImmutableArray<Diagnostic> Compile(
        IReadOnlyList<(string FileName, string Resx, Declaration Declaration)> resources, string source)
    {
        var files = Files(resources);

        CSharpGeneratorDriver
            .Create(
                [new VocabularyGenerator().AsSourceGenerator(), new KeyTypeGenerator().AsSourceGenerator()],
                files.Texts,
                optionsProvider: files.Options)
            .RunGeneratorsAndUpdateCompilation(Compile(source), out var updated, out _);

        return updated.GetDiagnostics();
    }

    /// <summary>Runs the key-type generator over <paramref name="source"/>.</summary>
    public static Run GenerateKeyTypes(string source)
    {
        var driver = CSharpGeneratorDriver
            .Create([new KeyTypeGenerator().AsSourceGenerator()])
            .RunGeneratorsAndUpdateCompilation(Compile(source), out _, out var diagnostics);

        return new Run(
            diagnostics,
            driver.GetRunResult().Results
                .SelectMany(result => result.GeneratedSources)
                .ToDictionary(generated => generated.HintName, generated => generated.SourceText.ToString()));
    }

    /// <summary>Runs an analyzer over a compilation, with the resources and declaration a build would supply.</summary>
    public static ImmutableArray<Diagnostic> Analyze(
        DiagnosticAnalyzer analyzer,
        string source,
        IReadOnlyList<(string FileName, string Resx, Declaration Declaration)>? resources = null)
    {
        var files = Files(resources ?? []);

        return Compile(source)
            .WithAnalyzers(
                [analyzer],
                new AnalyzerOptions(files.Texts, files.Options))
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Key types as the generator writes them. Spelled out rather than generated, because an analyzer test
    /// runs one analyzer over one compilation and nothing has written the bodies.
    /// </summary>
    public const string KeyTypes = """
        using SimpleLocalizations;

        namespace Probe;

        [VocabularyKey]
        public readonly struct ProbeKey
        {
            private ProbeKey(string key) => Key = key;
            public string Key { get; }
            public static ProbeKey From(string key) => new(key);
        }

        [VocabularyKey]
        public readonly struct OtherKey
        {
            private OtherKey(string key) => Key = key;
            public string Key { get; }
            public static OtherKey From(string key) => new(key);
        }
        """;

    /// <summary>A resource file holding <paramref name="entries"/>, as key/value/comment triples.</summary>
    public static string Resx(params (string Key, string Value, string? Comment)[] entries)
    {
        var data = entries.Select(entry =>
            $"""
               <data name="{entry.Key}" xml:space="preserve">
                 <value>{entry.Value}</value>
                 {(entry.Comment is null ? "" : $"<comment>{entry.Comment}</comment>")}
               </data>
             """);

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
              <resheader name="version"><value>2.0</value></resheader>
            {string.Join("\n", data)}
            </root>
            """;
    }

    /// <summary>A resource holding keys with no words and no notes, which is most of what a rule needs.</summary>
    public static string Keys(params string[] keys) =>
        Resx([.. keys.Select(key => (key, "words", (string?)null))]);

    private static CSharpCompilation Compile(string source)
    {
        var references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat([MetadataReference.CreateFromFile(typeof(VocabularyKeyAttribute).Assembly.Location)])
            .Cast<MetadataReference>()
            .Distinct();

        return CSharpCompilation.Create(
            "Probe",
            source.Length == 0 ? [] : [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static (ImmutableArray<AdditionalText> Texts, AnalyzerConfigOptionsProvider Options) Files(
        IReadOnlyList<(string FileName, string Resx, Declaration Declaration)> resources)
    {
        var texts = resources
            .Select(resource => (AdditionalText)new Text(resource.FileName, resource.Resx))
            .ToImmutableArray();

        var declared = texts
            .Select((text, index) => (text, resources[index].Declaration))
            .ToDictionary(pair => pair.text, pair => pair.Declaration);

        return (texts, new Options(declared));
    }

    private sealed class Text(string path, string content) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(content);
    }

    private sealed class Options(
        IReadOnlyDictionary<AdditionalText, Declaration> declared) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = Config.Empty;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Config.Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            if (!declared.TryGetValue(textFile, out var declaration))
            {
                return Config.Empty;
            }

            return new Config(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["build_metadata.AdditionalFiles.VocabularyClass"] = declaration.Class,
                ["build_metadata.AdditionalFiles.VocabularyKeyType"] = declaration.KeyType,
                ["build_metadata.AdditionalFiles.VocabularyNamespace"] = declaration.Namespace,
                ["build_metadata.AdditionalFiles.VocabularyDerived"] = declaration.Derived,
                ["build_metadata.AdditionalFiles.VocabularyResourceName"] = declaration.ResourceName,
                ["build_metadata.AdditionalFiles._VocabularyHint"] = declaration.Hint,
            });
        }

        private sealed class Config(Dictionary<string, string> values) : AnalyzerConfigOptions
        {
            public static readonly Config Empty = new([]);

            public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value!);
        }
    }
}
