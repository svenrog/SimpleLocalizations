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
/// patterns for are therefore this package's, and a consumer needing another ships a satellite beside it.
/// </para>
/// </summary>
public static class ListFormatter
{
    private static readonly StringCatalog _catalog = new(
        "SimpleLocalizations.ListPatterns",
        typeof(ListFormatter).Assembly,
        CultureInfo.GetCultureInfo("en-US"));

    /// <summary>
    /// The items as a conjunction list — <c>"a"</c>, <c>"a and b"</c>, <c>"a, b, and c"</c>. Empty input is an
    /// empty string, so a caller interpolating it produces a short sentence rather than a broken one.
    /// </summary>
    public static string And(IEnumerable<string> items) => Joined(items, "List_And");

    /// <summary>
    /// The items as a disjunction list — <c>"a or b"</c>, <c>"a, b, or c"</c>. The serial comma is the same
    /// rule as <see cref="And"/>'s, so the alternatives an error offers cannot be spelled one way and the
    /// prose around them another.
    /// </summary>
    public static string Or(IEnumerable<string> items) => Joined(items, "List_Or");

    private static string Joined(IEnumerable<string> items, string patterns)
    {
        var list = items.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();

        return list.Count switch
        {
            0 => string.Empty,
            1 => list[0],
            2 => Format($"{patterns}_Two", list[0], list[1]),
            // Folded left so the pattern applies pairwise, which is how ICU's list patterns compose: the
            // accumulated head is always the first argument, the next item the second.
            _ => Format(
                $"{patterns}_End",
                list.Take(list.Count - 1).Skip(1)
                    .Aggregate(list[0], (head, item) => Format($"{patterns}_Middle", head, item)),
                list[list.Count - 1]),
        };
    }

    /// <summary>
    /// The first <paramref name="max"/> items, plain-joined, with a count of what was dropped — the shape a
    /// long list takes when naming every member would bury the point. Under the cap it is a plain join, not a
    /// conjunction list: this is an enumeration the reader scans, not a sentence.
    /// </summary>
    public static string Truncated(IReadOnlyList<string> items, int max)
    {
        if (items.Count <= max)
        {
            return Join(items);
        }

        return Format(
            "List_Truncated",
            Join([.. items.Take(max)]),
            (items.Count - max).ToString(CultureInfo.CurrentCulture));
    }

    /// <summary>A plain separated join, pattern-driven so a culture that separates differently can say so.</summary>
    private static string Join(IReadOnlyList<string> items) =>
        items.Count == 0 ? string.Empty : items.Skip(1).Aggregate(items[0],
            (head, item) => Format("List_Separator", head, item));

    private static string Format(string key, string first, string second) =>
        string.Format(CultureInfo.CurrentCulture, _catalog.Get(key), first, second);
}
