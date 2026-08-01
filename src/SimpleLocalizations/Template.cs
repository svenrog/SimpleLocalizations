using System.Globalization;

namespace SimpleLocalizations;

/// <summary>
/// Fills a resource template's holes. One place because every edge that does it — a read edge rendering for
/// whoever is reading now, and the neutral rendering that will be stored — must fill identically, or the same
/// key produces two different sentences.
/// </summary>
public static class Template
{
    /// <summary>
    /// <paramref name="template"/> with <paramref name="args"/> filled in, formatted for
    /// <paramref name="culture"/> — the reader's where a sentence is about to be read, the invariant one
    /// where it is about to be stored.
    /// <para>
    /// A template with more holes than it was given args <b>throws</b>, including when it was given none. An
    /// empty list is not a reason to skip the fill: the one caller that passes no args is the stored
    /// rendering, and a literal <c>{0}</c> reaching a database is the failure this library exists to prevent.
    /// </para>
    /// </summary>
    public static string Fill(CultureInfo culture, string template, IReadOnlyList<object?> args) =>
        string.Format(culture, template, [.. args]);
}
