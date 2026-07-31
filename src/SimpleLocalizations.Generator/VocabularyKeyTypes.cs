namespace SimpleLocalizations.Generator;

/// <summary>
/// The key type each family of a vocabulary produces — a default, and the families that differ from it.
/// </summary>
internal sealed class VocabularyKeyTypes
{
    private readonly IReadOnlyDictionary<string, string> _byFamily;

    private VocabularyKeyTypes(string fallback, IReadOnlyDictionary<string, string> byFamily)
    {
        Fallback = fallback;
        _byFamily = byFamily;
    }

    /// <summary>What a family with no entry of its own produces.</summary>
    public string Fallback { get; }

    /// <summary>The type <paramref name="key"/>'s family produces, matched on its first segment.</summary>
    public string For(string key)
    {
        var family = key.Split('.')[0];
        return _byFamily.TryGetValue(family, out var type) ? type : Fallback;
    }

    /// <summary>
    /// Which key type each family produces. One file holds one project's whole vocabulary — the words a
    /// translator edits belong together — but the families in it are not the same kind of thing, and a key
    /// type standing in for another resolves to nothing. Stated as a default followed by the families that
    /// differ: <c>LocalizationKey | detail=DetailLabel | signal=SignalKey</c>.
    /// <para>
    /// Separated by <c>|</c> rather than the <c>;</c> MSBuild reads as a list, for the reason
    /// <c>SL1009</c> exists: the metadata reaches here through a generated <c>.editorconfig</c>, where
    /// <c>;</c> begins a comment and everything after the first family is dropped before anything sees it.
    /// </para>
    /// <para>
    /// <see langword="null"/> when the declaration is not that shape. The default is required: without one, a
    /// key outside every listed family has no type to name.
    /// </para>
    /// </summary>
    public static VocabularyKeyTypes? Parse(string declared)
    {
        var fallback = "";
        var byFamily = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in declared.Split(['|'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split('=');

            if (parts.Length == 2 && parts[0].Trim().Length > 0 && parts[1].Trim().Length > 0)
            {
                byFamily[parts[0].Trim()] = parts[1].Trim();
            }
            else if (parts.Length == 1 && entry.Trim().Length > 0 && fallback.Length == 0)
            {
                fallback = entry.Trim();
            }
            else
            {
                return null;
            }
        }

        return fallback.Length == 0 ? null : new VocabularyKeyTypes(fallback, byFamily);
    }
}
