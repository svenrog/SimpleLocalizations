using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SimpleLocalizations.Generator;
using System.Collections.Immutable;

namespace SimpleLocalizations.Benchmarks;

/// <summary>
/// What the build half costs over a vocabulary of a given size — the cold run a build pays once, and the
/// second run an IDE pays on every keystroke.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class GeneratorBenchmarks
{
    private CSharpCompilation _compilation = null!;
    private ImmutableArray<AdditionalText> _texts;
    private AnalyzerConfigOptionsProvider _options = null!;
    private GeneratorDriver _warm = null!;
    private CSharpCompilation _edited = null!;

    /// <summary>
    /// Keys in the resource: a screen's worth, a feature's, a whole product's, and one nobody should author.
    /// The last is there to prove there is no cliff rather than because anyone will meet it — a hundred
    /// thousand keys is a resource file no translator could hold, and the component should still answer.
    /// </summary>
    [Params(10, 1_000, 100_000)]
    public int Keys { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _compilation = CSharpCompilation.Create(
            "Bench",
            [CSharpSyntaxTree.ParseText(_set)],
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
                // Named rather than left to the loaded set: assemblies load lazily, so a run that touches no
                // type from the package would not reference it, [VocabularyFamily] would not resolve, and
                // both rules would find no set and return having measured nothing.
                .Concat([MetadataReference.CreateFromFile(typeof(VocabularyFamilyAttribute).Assembly.Location)])
                .Distinct(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // A benchmark over a compilation that does not compile measures the early return.
        var broken = _compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (broken.Count > 0)
        {
            throw new InvalidOperationException(
                $"the benchmark's compilation does not compile: {string.Join("; ", broken)}");
        }

        _texts = [new Resource("Vocab.resx", Resx(Keys))];
        _options = new Options();

        _warm = Driver().RunGenerators(_compilation);
        _edited = _compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Unrelated { }"));
    }

    /// <summary>The run a build pays: nothing cached, every key read and emitted.</summary>
    [Benchmark(Baseline = true)]
    public GeneratorDriver Cold() => Driver().RunGenerators(_compilation);

    /// <summary>
    /// The run an IDE pays on a keystroke that touched nothing the generator reads. This is the number that
    /// decides whether the component is noticed, and it should be close to nothing.
    /// </summary>
    [Benchmark]
    public GeneratorDriver Incremental() => _warm.RunGenerators(_edited);

    [Benchmark]
    public ImmutableArray<Diagnostic> FamilyRule() => Analyze(new VocabularyKeyFamilies());

    [Benchmark]
    public ImmutableArray<Diagnostic> HeadingRule() => Analyze(new VocabularyHeadings());

    private ImmutableArray<Diagnostic> Analyze(DiagnosticAnalyzer analyzer) =>
        _compilation
            .WithAnalyzers([analyzer], new AnalyzerOptions(_texts, _options))
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();

    private GeneratorDriver Driver() =>
        CSharpGeneratorDriver.Create(
            [new VocabularyGenerator().AsSourceGenerator()], _texts, optionsProvider: _options);

    private static string Resx(int keys) =>
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <root>
        {string.Join("\n", Enumerable.Range(0, keys).Select(index =>
            $"""  <data name="section{index % 20}.key-{index}"><value>words</value></data>"""))}
        </root>
        """;

    private const string _set = """
        using SimpleLocalizations;

        namespace Bench;

        [VocabularyFamily("section")]
        public enum Sections { Section0, Section1, Section2 }

        [VocabularyKey(Families = typeof(Sections))]
        public readonly struct FiledKey
        {
            private FiledKey(string key) => Key = key;
            public string Key { get; }
            public static FiledKey From(string key) => new(key);
        }
        """;

    private sealed class Resource(string path, string content) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(content);
    }

    private sealed class Options : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Config([]);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new Config([]);

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
            new Config(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["build_metadata.AdditionalFiles.VocabularyClass"] = "BenchKeys",
                ["build_metadata.AdditionalFiles.VocabularyKeyType"] = "Bench.FiledKey",
                ["build_metadata.AdditionalFiles.VocabularyNamespace"] = "Bench",
            });

        private sealed class Config(Dictionary<string, string> values) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value!);
        }
    }
}
