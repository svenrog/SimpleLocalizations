using BenchmarkDotNet.Attributes;
using System.Globalization;

namespace SimpleLocalizations.Benchmarks;

/// <summary>
/// What the runtime costs on a render path: resolving a key, composing a sentence, joining a list, and
/// picking a culture for a request.
/// </summary>
[MemoryDiagnoser]
public class RuntimeBenchmarks
{
    private static readonly TextCultures _cultures = new("en-US", "en-GB", "sv-SE");

    private static readonly StringCatalog _catalog = _cultures.Catalog(
        "SimpleLocalizations.Benchmarks.BenchmarkStrings", typeof(RuntimeBenchmarks).Assembly);

    private static readonly string[] _three = ["alpha", "beta", "gamma"];

    private string[] _items = [];

    /// <summary>How many items the list benchmarks join. Three is a sentence; twenty is a report.</summary>
    [Params(3, 20)]
    public int Items { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _items = [.. Enumerable.Range(0, Items).Select(index => $"item{index}")];

        // The first lookup loads the resource set. Benchmarking that would measure ResourceManager's cold
        // start once and the thing being asked about never.
        _ = _catalog.Get("greeting");
    }

    [Benchmark(Baseline = true)]
    public string Get() => _catalog.Get("greeting");

    [Benchmark]
    public string GetTyped() => _catalog.Get(LocalizationKey.From("greeting"));

    [Benchmark]
    public string Format() => _catalog.Format("composed", 1, 3);

    [Benchmark]
    public string Neutral() => _catalog.Neutral("composed", "1", "3");

    [Benchmark]
    public LocalizedText Say() => _catalog.Say("composed", 1, 3);

    [Benchmark]
    public string ListAnd() => ListFormatter.And(_items);

    [Benchmark]
    public string ListTruncated() => ListFormatter.Truncated(_items, 3);

    [Benchmark]
    public CultureInfo NegotiateExact() => _cultures.Negotiate(_svSe);

    [Benchmark]
    public CultureInfo NegotiateLanguageOnly() => _cultures.Negotiate(_sv);

    [Benchmark]
    public CultureInfo NegotiateNoMatch() => _cultures.Negotiate(_deFr);

    private static readonly string[] _svSe = ["sv-SE"];

    private static readonly string[] _sv = ["sv"];

    private static readonly string[] _deFr = ["de", "fr"];

    /// <summary>A plain join, for the difference between punctuation and grammar in a number.</summary>
    [Benchmark]
    public string PlainJoin() => string.Join(", ", _three);
}
