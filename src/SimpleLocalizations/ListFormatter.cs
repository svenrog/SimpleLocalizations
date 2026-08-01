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
    /// the next item — except <c>list-truncated</c>, whose <c>{1}</c> is the count that was dropped.
    /// <para>
    /// Named here because passing a catalog makes them a contract rather than an implementation detail: a
    /// consumer's parity test reads this, and a key added to the package without one added to the list would
    /// throw in someone else's build.
    /// </para>
    /// <para>
    /// Lowercase, and <b>flat</b>: a dot is the one piece of notation that becomes structure here — it
    /// generates a nested class, a <c>Prefix</c> and a <c>Covers</c> — and nothing is generated from these.
    /// A dotted spelling would promise a nesting that never arrives, so the words are hyphenated inside one
    /// segment, which is what this package spells everywhere a key names no family.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> Patterns { get; } =
    [
        "list-and-two", "list-and-middle", "list-and-end",
        "list-or-two", "list-or-middle", "list-or-end",
        "list-separator", "list-truncated",
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
        Joined(items, "list-and-two", "list-and-middle", "list-and-end", patterns ?? _shipped);

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
        Joined(items, "list-or-two", "list-or-middle", "list-or-end", patterns ?? _shipped);

    /// <summary>
    /// The keys are passed whole rather than composed from a stem. Interpolating one per call allocated a
    /// key string on a path a page walks for every list it renders, to arrive at one of six constants.
    /// </summary>
    private static string Joined(
        IEnumerable<string> items, string two, string middle, string end, StringCatalog patterns)
    {
        var list = Present(items);

        return list.Count switch
        {
            0 => string.Empty,
            1 => list[0],
            2 => Format(patterns, two, list[0], list[1]),
            _ => Format(patterns, end, Folded(list, middle, patterns), list[list.Count - 1]),
        };
    }

    /// <summary>
    /// The items that are actually words. Filtered by hand because a LINQ predicate allocates an iterator
    /// and a closure, and nothing here needs deferred execution — the count is read immediately.
    /// </summary>
    private static List<string> Present(IEnumerable<string> items)
    {
        var present = new List<string>();

        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                present.Add(item);
            }
        }

        return present;
    }

    /// <summary>
    /// Everything but the last item, folded left so the pattern applies pairwise — which is how ICU's list
    /// patterns compose: the accumulated head is always the first argument, the next item the second.
    /// </summary>
    private static string Folded(List<string> list, string middle, StringCatalog patterns)
    {
        var head = list[0];

        for (var index = 1; index < list.Count - 1; index++)
        {
            head = Format(patterns, middle, head, list[index]);
        }

        return head;
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
            return Join(items, items.Count, catalog);
        }

        return Format(
            catalog,
            "list-truncated",
            Join(items, max, catalog),
            (items.Count - max).ToString(CultureInfo.CurrentCulture));
    }

    /// <summary>
    /// A plain separated join of the first <paramref name="count"/> items, pattern-driven so a culture that
    /// separates differently can say so. Counted rather than copied: the truncating caller wants a prefix of
    /// the list, and taking one used to allocate a second list to hold it.
    /// </summary>
    private static string Join(IReadOnlyList<string> items, int count, StringCatalog patterns)
    {
        if (count == 0)
        {
            return string.Empty;
        }

        var head = items[0];

        for (var index = 1; index < count; index++)
        {
            head = Format(patterns, "list-separator", head, items[index]);
        }

        return head;
    }

    private static string Format(StringCatalog patterns, string key, string first, string second) =>
        string.Format(CultureInfo.CurrentCulture, patterns.Get(key), first, second);
}
