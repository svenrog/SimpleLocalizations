namespace SimpleLocalizations.Generator;

/// <summary>
/// The <c>left=right|left=right</c> shape both family rules are declared in, read one way so the two cannot
/// disagree about what a declaration means.
/// <para>
/// Separated by <c>|</c> for the reason <see cref="VocabularyGenerator"/> states: <c>;</c> never survives the
/// generated <c>.editorconfig</c> the metadata travels in, which is what <c>SL1009</c> is for.
/// </para>
/// </summary>
internal static class PairList
{
    /// <summary>
    /// The pairs in <paramref name="declared"/>. An entry that is not a single <c>=</c> with both sides
    /// present is skipped: these declarations are optional, so a malformed one turns its rule off rather than
    /// failing a build over a rule the project never asked for.
    /// </summary>
    public static IEnumerable<KeyValuePair<string, string>> Read(string? declared)
    {
        foreach (var entry in (declared ?? "").Split(['|'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split('=');

            if (parts.Length == 2 && parts[0].Trim().Length > 0 && parts[1].Trim().Length > 0)
            {
                yield return new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim());
            }
        }
    }
}
