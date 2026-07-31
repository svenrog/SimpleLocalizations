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
    /// <paramref name="template"/> with <paramref name="args"/> filled in. Args are already strings — a
    /// producer formats its own numbers and dates, because it knows which are data and which are counts —
    /// so <paramref name="culture"/> decides nothing today and is passed anyway: it is what the caller is
    /// claiming about the sentence, and a template that grows a numeric hole must not silently pick one.
    /// </summary>
    public static string Fill(CultureInfo culture, string template, IReadOnlyList<string> args) =>
        args.Count == 0 ? template : string.Format(culture, template, [.. args]);
}
