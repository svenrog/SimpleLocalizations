namespace SimpleLocalizations.Tests.Performance;

/// <summary>
/// What the runtime costs per call, held to a ceiling.
/// <para>
/// Ceilings rather than exact figures: an allocation count moves with the framework, and a test asserting
/// the current one to the byte fails on a runtime upgrade for no reason anybody cares about. Each is set
/// well above what the call actually allocates, so it catches a change of <em>shape</em> — a LINQ chain
/// reintroduced on a render path, a split per candidate, a key composed per call — and nothing smaller.
/// </para>
/// <para>
/// Bytes rather than elapsed time, so these are safe to fail a build on. See <see cref="Allocation"/>.
/// </para>
/// </summary>
[Collection(CultureCollection.Name)]
public class AllocationTests
{
    private static readonly TextCultures _cultures = new("en-US", "sv-SE");

    private static readonly StringCatalog _catalog =
        _cultures.Catalog("SimpleLocalizations.Tests.CatalogFixture", typeof(AllocationTests).Assembly);

    private static readonly string[] _ten = [.. Enumerable.Range(0, 10).Select(index => $"item{index}")];

    [Fact]
    public void A_lookup_allocates_nothing()
    {
        // The hottest call in the package, and the one a render path repeats: ResourceManager caches its set,
        // so resolving a key that is already loaded should cost nothing at all.
        Under(0, "Get(string)", () => _catalog.Get("greeting"));
        Under(0, "Get(key)", () => _catalog.Get(LocalizationKey.From("greeting")));
        Under(0, "TryGet", () => _catalog.TryGet("greeting", out _));
    }

    [Fact]
    public void Composing_a_sentence_costs_its_arguments_and_its_result()
    {
        Under(512, "Format", () => _catalog.Format("composed", 1, 3));
        Under(512, "Neutral", () => _catalog.Neutral("composed", "1", "3"));
        Under(512, "Say", () => _catalog.Say("composed", 1, 3));
    }

    [Fact]
    public void Joining_a_list_scales_with_the_fold_and_nothing_else()
    {
        Under(768, "And(3)", () => ListFormatter.And(["a", "b", "c"]));
        Under(768, "Or(3)", () => ListFormatter.Or(["a", "b", "c"]));
        Under(768, "Truncated(10, 3)", () => ListFormatter.Truncated(_ten, 3));
    }

    [Fact]
    public void Resolving_a_culture_does_not_allocate_per_candidate()
    {
        // The regression this exists for: comparing language subtags by splitting them cost an array and two
        // strings for every supported culture, on a path that runs once per request.
        Under(0, "TryResolve, no match", () => _cultures.TryResolve("de-DE", out _));
        Under(384, "TryResolve, match", () => _cultures.TryResolve("sv-SE", out _));
        Under(384, "Negotiate, exact", () => _cultures.Negotiate(["sv-SE"]));
        Under(384, "Negotiate, language only", () => _cultures.Negotiate(["sv"]));
        Under(384, "Negotiate, no match", () => _cultures.Negotiate(["de", "fr"]));
    }

    private static void Under(long ceiling, string what, Action work)
    {
        var bytes = Allocation.PerCall(work);

        Assert.True(bytes <= ceiling, $"{what} allocated {bytes} B/op, ceiling {ceiling} B");
    }
}
