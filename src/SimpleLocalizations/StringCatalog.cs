using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace SimpleLocalizations;

/// <summary>
/// One project's localized text, over the embedded resource set an assembly ships.
/// <para>
/// A key absent from <em>every</em> culture throws, deliberately: a missing resource is an authoring defect
/// fixed at build time, not a runtime condition — and text that silently renders as its own key is how a
/// half-translated build reaches a customer-facing surface unnoticed. A key present in the neutral set but
/// absent from a culture never reaches that path: <see cref="ResourceManager"/> falls back, which is what
/// makes a culture file an override list rather than a copy.
/// </para>
/// </summary>
public sealed class StringCatalog
{
    private readonly ResourceManager _resources;
    private readonly CultureInfo _neutral;

    /// <summary>
    /// Creates a catalog over <paramref name="baseName"/> in <paramref name="assembly"/>, whose unsuffixed
    /// resource set is <paramref name="neutral"/>. Usually reached through <see cref="TextCultures.Catalog"/>.
    /// </summary>
    public StringCatalog(string baseName, Assembly assembly, CultureInfo neutral)
    {
        _resources = new ResourceManager(baseName, assembly);
        _neutral = neutral;
    }

    /// <summary>The text for <paramref name="key"/> in the ambient UI culture.</summary>
    public string Get(string key) => Get(key, CultureInfo.CurrentUICulture);

    /// <summary>
    /// <see cref="Get(string)"/> in a named culture, for text that must <em>not</em> move with the reader —
    /// the neutral rendering stored beside a record, which is compared and persisted rather than displayed.
    /// </summary>
    public string Get(string key, CultureInfo culture) =>
        TryGet(key, culture, out var value)
            ? value
            : throw new MissingManifestResourceException(
                $"no resource '{key}' in '{_resources.BaseName}' for any culture");

    /// <summary>
    /// <see cref="Get(string)"/> for a key that is legitimately optional — one derived from data rather than
    /// authored, where absence is a normal result instead of the defect <see cref="Get(string)"/> throws on.
    /// </summary>
    public bool TryGet(string key, out string value) => TryGet(key, CultureInfo.CurrentUICulture, out value);

    /// <summary>
    /// <see cref="TryGet(string, out string)"/> in a named culture.
    /// <para>
    /// An <b>empty</b> value reads as unauthored, which is how a key says "the identity is mine, the words
    /// are the data's" — a record titled by the thing it detected, a contact titled by a person's name. The
    /// key has to be declared, because it is stored and matched; translating its words would be wrong rather
    /// than missing, so the resource file states that by authoring none.
    /// </para>
    /// </summary>
    public bool TryGet(string key, CultureInfo culture, out string value)
    {
        var found = _resources.GetString(key, culture);
        value = found ?? "";
        return value.Length > 0;
    }

    /// <summary>
    /// The one way text that will be <b>stored</b> is composed: the neutral culture's words, filled
    /// invariantly. Every stored form goes through this, so two of them cannot disagree about what "stored"
    /// means. Throws on an unauthored key, like every write-time lookup.
    /// </summary>
    /// <remarks>
    /// The neutral culture because a reader's copy is composed again from the key at the read edge; invariant
    /// filling because an interpolated number belongs to no reader either, and a stored string is what the
    /// identity every verdict is filed under is made of.
    /// <para>
    /// A template with holes and no args to fill them throws, like every other fill: a literal <c>{0}</c>
    /// reaching a database is not recoverable, and this is the path that writes one there.
    /// </para>
    /// </remarks>
    public string Neutral(string key, params string[] args) =>
        Template.Fill(CultureInfo.InvariantCulture, Get(key, _neutral), args);

    /// <summary>
    /// <see cref="Get(string)"/> composed with <paramref name="args"/>. Formatted with the ambient
    /// <see cref="CultureInfo.CurrentCulture"/>, so an interpolated date or number reads the way the rest of
    /// the sentence does.
    /// </summary>
    public string Format(string key, params object?[] args) =>
        Template.Fill(CultureInfo.CurrentCulture, Get(key), args);

    /// <summary>
    /// <see cref="Format(string, object?[])"/> as a <see cref="LocalizedText"/> — the same sentence, carrying the proof that it
    /// came from here. Reached for where a surface renders straight into the reader's culture and a call site
    /// must not be able to word the sentence itself.
    /// </summary>
    public LocalizedText Say(string key, params object?[] args) => LocalizedText.From(this, key, args);

    /// <summary>The text for <paramref name="key"/> in the ambient UI culture.</summary>
    public string Get(LocalizationKey key) => Get(key.Key);

    /// <summary><see cref="Get(LocalizationKey)"/> composed with <paramref name="args"/>.</summary>
    public string Format(LocalizationKey key, params object?[] args) => Format(key.Key, args);

    /// <summary>The neutral rendering of <paramref name="key"/>, for text about to be stored.</summary>
    public string Neutral(LocalizationKey key, params string[] args) => Neutral(key.Key, args);

    /// <summary><see cref="Format(LocalizationKey, object?[])"/> as a <see cref="LocalizedText"/>.</summary>
    public LocalizedText Say(LocalizationKey key, params object?[] args) => Say(key.Key, args);

    /// <summary>
    /// The keys authored for exactly <paramref name="culture"/>, without inheriting the neutral set. A parity
    /// test reads this to prove a culture file overrides only keys that exist; nothing else needs it.
    /// </summary>
    public IReadOnlyCollection<string> AuthoredKeys(CultureInfo culture)
    {
        // The neutral set is the unsuffixed resx, which ships as the assembly's *invariant* resource. Asking
        // for it under its culture name looks for a satellite that does not exist and yields nothing, which
        // reads as "the neutral set authors no keys" rather than as an error.
        var authored = culture.Name == _neutral.Name ? CultureInfo.InvariantCulture : culture;

        // Not disposed: the set belongs to the ResourceManager's own cache, so disposing it here closes the
        // one every later Get on this catalog reads through.
        var set = _resources.GetResourceSet(authored, createIfNotExists: true, tryParents: false);
        if (set is null)
        {
            return [];
        }

        return [.. set.Cast<DictionaryEntry>().Select(entry => (string)entry.Key)];
    }
}
