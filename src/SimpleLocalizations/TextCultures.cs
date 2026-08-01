using System.Globalization;
using System.Reflection;

namespace SimpleLocalizations;

/// <summary>
/// The cultures an application ships text for, and the one place a requested culture is resolved onto them.
/// <para>
/// <see cref="Neutral"/> is the unsuffixed resource set: every key is authored there, and a culture file
/// carries only the entries that actually differ. A key missing from a culture file therefore falls back to
/// correct neutral text rather than to nothing, which is what lets a culture author nothing at all for the
/// entries it spells the neutral way.
/// </para>
/// <para>
/// Constructed rather than discovered, and required by every <see cref="StringCatalog"/>: an application
/// whose neutral set is not English and is assumed to be English stores the wrong words under the right key,
/// which is the failure this library exists to prevent.
/// </para>
/// </summary>
public sealed class TextCultures
{
    /// <summary>
    /// Declares <paramref name="neutral"/> as the unsuffixed set and <paramref name="others"/> as the
    /// cultures shipping a file beside it. Both are resolved eagerly, so a name no culture answers to fails
    /// where it is configured rather than at the first lookup.
    /// </summary>
    public TextCultures(string neutral, params string[] others)
    {
        NeutralCulture = CultureInfo.GetCultureInfo(neutral);
        Neutral = NeutralCulture.Name;

        var supported = new List<string> { Neutral };

        foreach (var other in others ?? [])
        {
            var name = CultureInfo.GetCultureInfo(other).Name;

            if (!supported.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                supported.Add(name);
            }
        }

        Supported = supported;
    }

    /// <summary>The neutral culture's name. Text authored without a culture suffix is this.</summary>
    public string Neutral { get; }

    /// <summary>The neutral culture, for the lookups that must not follow the reader.</summary>
    public CultureInfo NeutralCulture { get; }

    /// <summary>Every culture with a resource set, neutral first.</summary>
    public IReadOnlyList<string> Supported { get; }

    /// <summary>A catalog over <paramref name="baseName"/> in <paramref name="assembly"/>, neutral set here.</summary>
    public StringCatalog Catalog(string baseName, Assembly assembly) =>
        new(baseName, assembly, NeutralCulture);

    /// <summary>
    /// Resolves an explicitly requested culture, failing rather than falling back. An operator who asked for
    /// one language must not silently get another: the report would look fine and be in the wrong language.
    /// <para>
    /// Answers <see langword="bool"/> and no message. A refusal is a sentence, and this library authors no
    /// resource the caller's reader can translate — wording it belongs to whoever owns a vocabulary.
    /// </para>
    /// </summary>
    public bool TryResolve(string? requested, out CultureInfo culture)
    {
        culture = NeutralCulture;

        if (string.IsNullOrWhiteSpace(requested))
        {
            return true;
        }

        var match = Match(requested!.Trim());
        if (match is null)
        {
            return false;
        }

        culture = CultureInfo.GetCultureInfo(match);
        return true;
    }

    /// <summary>
    /// Picks the best supported culture from an ordered preference list, falling back to the neutral one.
    /// For content negotiation (an <c>Accept-Language</c> header), where a preference list is by definition a
    /// set of wishes rather than an instruction — unlike <see cref="TryResolve"/>.
    /// <para>
    /// A preference matches a culture that shares its <b>language</b> where none matches it exactly, so
    /// <c>sv</c> reaches an <c>sv-SE</c> set and <c>en-AU</c> reaches an <c>en-GB</c> one. A browser sends
    /// the bare language more often than not, and refusing it would answer a reader who asked for Swedish in
    /// English while holding Swedish. Each preference is tried both ways before the next is tried at all,
    /// because the order is the reader's ranking and an exact second choice does not outrank a near first.
    /// </para>
    /// </summary>
    public CultureInfo Negotiate(IEnumerable<string>? preferences)
    {
        foreach (var preference in preferences ?? [])
        {
            // Strip any RFC 9110 quality value; ordering is the caller's, which is what a parsed
            // Accept-Language already gives us.
            var tag = preference.Split(';')[0].Trim();

            if ((Match(tag) ?? Related(tag)) is { } match)
            {
                return CultureInfo.GetCultureInfo(match);
            }
        }

        return NeutralCulture;
    }

    /// <summary>
    /// Makes <paramref name="culture"/> the process-wide UI culture for text lookup, while leaving
    /// <see cref="CultureInfo.DefaultThreadCurrentCulture"/> alone: formatting culture is a separate axis, and
    /// a serialized payload is written invariantly regardless of who is reading.
    /// </summary>
    public static void Apply(CultureInfo culture) =>
        CultureInfo.DefaultThreadCurrentUICulture = culture;

    private string? Match(string tag) =>
        Supported.FirstOrDefault(c => string.Equals(c, tag, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The first supported culture sharing <paramref name="tag"/>'s language. First because
    /// <see cref="Supported"/> is neutral-first, which makes the pick the one the application would rather
    /// ship when it holds several of a language.
    /// </summary>
    private string? Related(string tag) =>
        Supported.FirstOrDefault(c => string.Equals(Language(c), Language(tag), StringComparison.OrdinalIgnoreCase));

    /// <summary>A tag's language subtag — everything before the first hyphen.</summary>
    private static string Language(string tag) => tag.Split('-')[0];
}
