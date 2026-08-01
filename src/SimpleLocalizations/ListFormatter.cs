using System.Globalization;

namespace SimpleLocalizations;

/// <summary>
/// Joins a list of items into prose for the ambient UI culture.
/// <para>
/// Exists because <c>string.Join(", ", items)</c> is not a translation-neutral operation and looks like one.
/// Where a list sits inside a sentence, the join carries grammar: en-US writes the serial (Oxford) comma
/// before the final conjunction and en-GB does not, so the same call has to produce <c>"a, b, and c"</c> for
/// one reader and <c>"a, b and c"</c> for the other. .NET ships no list formatter (ICU and Java both do), so
/// the patterns live in <c>ListPatterns.resx</c> and are applied here.
/// </para>
/// <para>
/// Only for lists that are <em>read as prose</em>. A structured enumeration — a census, an
/// items-after-a-colon list whose entries contain commas of their own — is punctuation rather than grammar
/// and should stay a plain join.
/// </para>
/// <para>
/// Every lookup here is ambient-culture, so this takes no <see cref="TextCultures"/>: it never renders the
/// neutral set on purpose, which is the only question that configuration answers. The cultures it ships
/// patterns for are this package's — en-US, en-GB and sv-SE. A consumer needing another authors the same
/// keys in a resource of its own and passes a catalog over it; see <see cref="Patterns"/>.
/// </para>
/// </summary>
public static class ListFormatter
{
    private static readonly StringCatalog _shipped = new(
        "SimpleLocalizations.ListPatterns",
        typeof(ListFormatter).Assembly,
        CultureInfo.GetCultureInfo("en-US"));

    /// <summary>
    /// The keys a catalog must author to stand in for this package's own, in the order a list uses them:
    /// the two-item form, the pattern folded over the middle, the one before the final conjunction, the
    /// plain separator, and the truncation. Each takes <c>{0}</c> as the accumulated head and <c>{1}</c> as
    /// the next item — except <c>List_Truncated</c>, whose <c>{1}</c> is the count that was dropped.
    /// <para>
    /// Named here because passing a catalog makes them a contract rather than an implementation detail: a
    /// consumer's parity test reads this, and a key added to the package without one added to the list would
    /// throw in someone else's build.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> Patterns { get; } =
    [
        "List_And_Two", "List_And_Middle", "List_And_End",
        "List_Or_Two", "List_Or_Middle", "List_Or_End",
        "List_Separator", "List_Truncated",
    ];

    /// <summary>
    /// The items as a conjunction list — <c>"a"</c>, <c>"a and b"</c>, <c>"a, b, and c"</c>. Empty input is an
    /// empty string, so a caller interpolating it produces a short sentence rather than a broken one.
    /// </summary>
    /// <param name="items">The items to join.</param>
    /// <param name="patterns">
    /// A catalog authoring <see cref="Patterns"/>, for a culture this package ships none for. The package's
    /// own when omitted.
    /// </param>
    public static string And(IEnumerable<string> items, StringCatalog? patterns = null) =>
        Joined(items, "List_And", patterns ?? _shipped);

    /// <summary>
    /// The items as a disjunction list — <c>"a or b"</c>, <c>"a, b, or c"</c>. The serial comma is the same
    /// rule as <see cref="And"/>'s, so the alternatives an error offers cannot be spelled one way and the
    /// prose around them another.
    /// </summary>
    /// <param name="items">The items to join.</param>
    /// <param name="patterns">
    /// A catalog authoring <see cref="Patterns"/>, for a culture this package ships none for. The package's
    /// own when omitted.
    /// </param>
    public static string Or(IEnumerable<string> items, StringCatalog? patterns = null) =>
        Joined(items, "List_Or", patterns ?? _shipped);

    private static string Joined(IEnumerable<string> items, string family, StringCatalog patterns)
    {
        var list = items.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();

        return list.Count switch
        {
            0 => string.Empty,
            1 => list[0],
            2 => Format(patterns, $"{family}_Two", list[0], list[1]),
            // Folded left so the pattern applies pairwise, which is how ICU's list patterns compose: the
            // accumulated head is always the first argument, the next item the second.
            _ => Format(
                patterns,
                $"{family}_End",
                list.Take(list.Count - 1).Skip(1)
                    .Aggregate(list[0], (head, item) => Format(patterns, $"{family}_Middle", head, item)),
                list[list.Count - 1]),
        };
    }

    /// <summary>
    /// The first <paramref name="max"/> items, plain-joined, with a count of what was dropped — the shape a
    /// long list takes when naming every member would bury the point. Under the cap it is a plain join, not a
    /// conjunction list: this is an enumeration the reader scans, not a sentence.
    /// </summary>
    /// <param name="items">The items to join.</param>
    /// <param name="max">How many to name before counting the rest.</param>
    /// <param name="patterns">
    /// A catalog authoring <see cref="Patterns"/>, for a culture this package ships none for. The package's
    /// own when omitted.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="max"/> is below one. A cap that names nothing is not a truncation — it rendered as a
    /// separator with no item before it — and a caller computing one from a count has a bug this hides.
    /// </exception>
    public static string Truncated(IReadOnlyList<string> items, int max, StringCatalog? patterns = null)
    {
        if (max < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(max), max, "a truncated list names at least one item");
        }

        var catalog = patterns ?? _shipped;

        if (items.Count <= max)
        {
            return Join(items, catalog);
        }

        return Format(
            catalog,
            "List_Truncated",
            Join([.. items.Take(max)], catalog),
            (items.Count - max).ToString(CultureInfo.CurrentCulture));
    }

    /// <summary>A plain separated join, pattern-driven so a culture that separates differently can say so.</summary>
    private static string Join(IReadOnlyList<string> items, StringCatalog patterns) =>
        items.Count == 0 ? string.Empty : items.Skip(1).Aggregate(items[0],
            (head, item) => Format(patterns, "List_Separator", head, item));

    private static string Format(StringCatalog patterns, string key, string first, string second) =>
        string.Format(CultureInfo.CurrentCulture, patterns.Get(key), first, second);
}
