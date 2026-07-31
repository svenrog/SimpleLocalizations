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
    /// Reports every collision under <paramref name="node"/>, whose children are emitted into a type named
    /// <paramref name="enclosing"/> — the generated class at the root, the parent's own member name below it.
    /// Returns whether any was found.
    /// </summary>
    internal static bool Check(
        VocabularyNode node, string enclosing, string fileName, Action<Diagnostic> report)
    {
        var found = false;
        var taken = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var child in node.Children)
        {
            var name = VocabularyMember.Name(child.Segment);

            if (name == enclosing)
            {
                report(Diagnostic.Create(
                    VocabularyDiagnostics.Shadows, Location.None, child.Path, fileName, name));
                found = true;
            }

            if (taken.TryGetValue(name, out var first))
            {
                report(Diagnostic.Create(
                    VocabularyDiagnostics.Collides, Location.None, child.Path, fileName, name, first));
                found = true;
            }
            else
            {
                taken.Add(name, child.Path);
            }

            found |= Check(child, name, fileName, report);
        }

        return found;
    }
}
