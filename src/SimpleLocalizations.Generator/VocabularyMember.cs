namespace SimpleLocalizations.Generator;

/// <summary>The member name a dotted segment takes, shared by the emitter and the collision check.</summary>
internal static class VocabularyMember
{
    /// <summary>
    /// Members the generated classes carry themselves, which a segment may therefore not be named: a family's
    /// <c>Prefix</c>, <c>Covers</c> and <c>Of</c>, and the root's <c>ResourceName</c> and <c>Catalog</c>.
    /// </summary>
    private static readonly HashSet<string> _reserved =
        new(StringComparer.Ordinal) { "Prefix", "Covers", "Of", "ResourceName", "Catalog" };

    /// <summary>
    /// A dotted segment as a member name: <c>not-http-only</c> becomes <c>NotHttpOnly</c>.
    /// <para>
    /// The first character is always uppercase or <c>_</c>, so no C# keyword can come out of this and no
    /// <c>@</c> escape is ever needed. It is not injective, though — a digit has no case, so <c>tls-1-0</c>
    /// and <c>tls10</c> both arrive here as <c>Tls10</c>, which is <see cref="VocabularyCollisions"/>' subject.
    /// </para>
    /// </summary>
    internal static string Name(string segment)
    {
        var name = Cased(segment);

        if (name.Length == 0 || char.IsDigit(name[0]))
        {
            name = "_" + name;
        }

        return _reserved.Contains(name) ? name + "Key" : name;
    }

    /// <summary>
    /// <paramref name="segment"/>'s parts run together with the first letter of each uppercased, a part being
    /// what lies between its hyphens and underscores. Written in one pass rather than as a split and a join:
    /// this is called once per node by the emitter and again by the collision walk, and every key in a
    /// resource is a node.
    /// </summary>
    private static string Cased(string segment)
    {
        var name = new char[segment.Length];
        var length = 0;
        var starting = true;

        foreach (var character in segment)
        {
            if (character is '-' or '_')
            {
                starting = true;
                continue;
            }

            name[length++] = starting ? char.ToUpperInvariant(character) : character;
            starting = false;
        }

        return new string(name, 0, length);
    }
}
