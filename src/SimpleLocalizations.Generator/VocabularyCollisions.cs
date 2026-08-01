using Microsoft.CodeAnalysis;

namespace SimpleLocalizations.Generator;

/// <summary>
/// The member names a tree of keys cannot legally take: one a sibling already produces, or one equal to the
/// class it nests in. Reported here rather than left to the compiler, because the error would land in
/// generated source and name no resource file.
/// </summary>
internal static class VocabularyCollisions
{
    /// <summary>
    /// Reports every collision under <paramref name="node"/> and drops the key that caused it, whose children
    /// are emitted into a type named <paramref name="enclosing"/> — the generated class at the root, the
    /// parent's own member name below it.
    /// <para>
    /// Dropped rather than refused wholesale: emitting both halves of a collision is invalid C#, but emitting
    /// neither costs every <em>other</em> key in the file a call site, which is the cascade this component
    /// exists to keep out of a consumer's build. The key that loses is the later of the two, and the
    /// diagnostic names it.
    /// </para>
    /// </summary>
    /// <returns>Whether any key was dropped, which is whether anything was reported.</returns>
    internal static bool Prune(
        VocabularyNode node, string enclosing, string fileName, Action<Diagnostic> report)
    {
        var taken = new Dictionary<string, string>(StringComparer.Ordinal);
        var dropped = new List<VocabularyNode>();
        var found = false;

        foreach (var child in node.Children)
        {
            var name = VocabularyMember.Name(child.Segment);

            if (name == enclosing)
            {
                report(Diagnostic.Create(
                    VocabularyDiagnostics.Shadows, Location.None, child.Path, fileName, name));
                dropped.Add(child);
                found = true;
                continue;
            }

            if (taken.TryGetValue(name, out var first))
            {
                report(Diagnostic.Create(
                    VocabularyDiagnostics.Collides, Location.None, child.Path, fileName, name, first));
                dropped.Add(child);
                continue;
            }

            taken.Add(name, child.Path);
            found |= Prune(child, name, fileName, report);
        }

        // After the walk: Children is the live collection the walk reads.
        foreach (var child in dropped)
        {
            node.Remove(child);
        }

        return found || dropped.Count > 0;
    }
}
