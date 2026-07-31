namespace SimpleLocalizations.Generator;

/// <summary>The member name a dotted segment takes, shared by the emitter and the collision check.</summary>
internal static class VocabularyMember
{
    /// <summary>Members every branch carries, which a segment may therefore not be named.</summary>
    private static readonly HashSet<string> _reserved = new(StringComparer.Ordinal) { "Prefix", "Covers" };

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
        var name = string.Concat(segment
            .Split('-', '_')
            .Where(part => part.Length > 0)
            .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));

        if (name.Length == 0 || char.IsDigit(name[0]))
        {
            name = "_" + name;
        }

        return _reserved.Contains(name) ? name + "Key" : name;
    }
}
