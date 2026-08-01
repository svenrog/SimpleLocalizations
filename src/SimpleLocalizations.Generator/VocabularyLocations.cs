using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SimpleLocalizations.Generator;

/// <summary>
/// Where a diagnostic about a resource points. A resx is an <c>AdditionalFile</c> rather than a syntax tree,
/// so nothing hands these out — but a diagnostic carrying no location at all cannot be navigated to, cannot
/// be suppressed with a <c>#pragma</c>, and names a key in a file the reader still has to search.
/// </summary>
internal static class VocabularyLocations
{
    /// <summary>The entry at <paramref name="span"/> of the resource at <paramref name="path"/>.</summary>
    public static Location Of(string path, SourceText text, LinePositionSpan span) =>
        Location.Create(path, text.Lines.GetTextSpan(span), span);

    /// <summary>
    /// The resource itself, for what is wrong with the file rather than with an entry in it: a declaration it
    /// does not carry, or words nothing can read.
    /// </summary>
    public static Location Of(string path) =>
        Location.Create(path, new TextSpan(0, 0), default);
}
