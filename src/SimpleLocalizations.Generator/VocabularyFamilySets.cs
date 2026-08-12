using System.Collections.Immutable;

namespace SimpleLocalizations.Generator;

/// <summary>
/// The declared sets a vocabulary is emitted against, by the family each is worded under.
/// <para>
/// A family two sets claim resolves to neither. Which one a lookup switched over would be a coin toss the
/// generated source states as fact, and both sets are held to the same words by <c>SL1011</c> — so
/// <c>SL1016</c> reports the pair where the types are, and nothing is emitted here.
/// </para>
/// </summary>
internal sealed class VocabularyFamilySets
{
    /// <summary>The set a family resolves to; absent where two claim it.</summary>
    private readonly Dictionary<string, FamilySet?> _byPrefix;

    public VocabularyFamilySets(ImmutableArray<FamilySet> declared)
    {
        _byPrefix = new Dictionary<string, FamilySet?>(StringComparer.Ordinal);

        foreach (var set in declared)
        {
            _byPrefix[set.Prefix] = _byPrefix.ContainsKey(set.Prefix) ? null : set;
        }
    }

    /// <summary>
    /// The set worded under <paramref name="path"/>, or <see langword="null"/> where none is or where the
    /// family is contested.
    /// </summary>
    public FamilySet? For(string path) => _byPrefix.TryGetValue(path, out var set) ? set : null;
}
