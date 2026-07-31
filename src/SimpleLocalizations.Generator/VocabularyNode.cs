namespace SimpleLocalizations.Generator;

/// <summary>
/// One dotted segment of a vocabulary, as the tree the generated classes mirror: <c>security.dns.spf.missing</c>
/// becomes <c>Security.Dns.Spf.Missing</c>.
/// <para>
/// A node is a leaf or a branch, never both — a key that is a proper prefix of another is reported rather
/// than generated, because a family prefix would match it and there would be no name left for it to take.
/// </para>
/// </summary>
internal sealed class VocabularyNode
{
    private readonly SortedDictionary<string, VocabularyNode> _children =
        new(StringComparer.Ordinal);

    private VocabularyNode(string segment, string path)
    {
        Segment = segment;
        Path = path;
    }

    /// <summary>The dotted segment this node is named by; empty at the root.</summary>
    public string Segment { get; }

    /// <summary>The full dotted key of this node — the family prefix a branch matches on.</summary>
    public string Path { get; }

    /// <summary>The authored key, when this node is a leaf.</summary>
    public VocabularyEntry? Entry { get; private set; }

    /// <summary>The nodes filed under this one, ordered by segment so the emitted source is stable.</summary>
    public IReadOnlyCollection<VocabularyNode> Children => _children.Values;

    public static VocabularyNode Root() => new("", "");

    /// <summary>
    /// Files <paramref name="entry"/> under its segments. Returns the node that was already taken, when the
    /// key collides with one that is a prefix of it or that it is a prefix of.
    /// </summary>
    public VocabularyNode? Add(VocabularyEntry entry)
    {
        var node = this;

        foreach (var segment in entry.Segments)
        {
            if (node.Entry is not null)
            {
                return node;
            }

            if (!node._children.TryGetValue(segment, out var child))
            {
                child = new VocabularyNode(segment, node.Path.Length == 0 ? segment : node.Path + "." + segment);
                node._children.Add(segment, child);
            }

            node = child;
        }

        if (node.Entry is not null || node._children.Count > 0)
        {
            return node;
        }

        node.Entry = entry;
        return null;
    }
}
