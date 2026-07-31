namespace SimpleLocalizations.Generator;

/// <summary>
/// One authored key and the note beside it — the resx <c>&lt;comment&gt;</c>, which becomes the generated
/// member's XmlDoc so a label's explanation sits next to the words rather than in a parallel C# file.
/// </summary>
internal sealed class VocabularyEntry
{
    public VocabularyEntry(string key, string? comment, string value)
    {
        Key = key;
        Comment = comment;
        Value = value;
    }

    /// <summary>The dotted key as the resource file spells it.</summary>
    public string Key { get; }

    /// <summary>
    /// The words authored for it. <b>Empty</b> is the declaration that the identity is the vocabulary's and
    /// the words are the data's — read by the runtime as absent, and available to a consumer's own rule.
    /// </summary>
    public string Value { get; }

    /// <summary>The note beside it, joined onto one line, or <see langword="null"/> when there is none.</summary>
    public string? Comment { get; }

    /// <summary><see cref="Key"/> split on its dots — the path the tree files it under, not a stored field.</summary>
    public string[] Segments => Key.Split('.');
}
