using Microsoft.CodeAnalysis.Text;
using System.Xml;
using System.Xml.Linq;

namespace SimpleLocalizations.Generator;

/// <summary>Reads the authored keys out of a <c>.resx</c>, with the note each carries.</summary>
internal static class VocabularyReader
{
    /// <summary>
    /// The string entries of <paramref name="resx"/>, or <see langword="null"/> when it is not a resource file
    /// at all — unparseable XML, or a root element that is not a resx <c>&lt;root&gt;</c>. Entries carrying a
    /// <c>type</c> or <c>mimetype</c> are not text — a resx can hold files and serialized objects — and a
    /// vocabulary is only ever text.
    /// <para>
    /// Unreadable is distinct from empty because the generator reports them separately: an empty list is a
    /// resource file that authors nothing, which is a different mistake from one nothing can read.
    /// </para>
    /// </summary>
    public static IReadOnlyList<VocabularyEntry>? Read(string resx)
    {
        XDocument document;
        try
        {
            // SetLineInfo so a diagnostic about an entry can point at it. Without it every refusal names a
            // key and leaves a reader to find it, and none of them can be navigated to or suppressed.
            document = XDocument.Parse(resx, LoadOptions.SetLineInfo);
        }
        catch (XmlException)
        {
            return null;
        }

        if (document.Root is not { } root || root.Name.LocalName != "root")
        {
            return null;
        }

        return
        [
            .. root
                .Elements("data")
                .Where(data => data.Attribute("type") is null && data.Attribute("mimetype") is null)
                .Select(data => (
                    Name: data.Attribute("name")?.Value,
                    Comment: data.Element("comment")?.Value,
                    Words: data.Element("value")?.Value,
                    Span: Span(data)))
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .Select(entry =>
                    new VocabularyEntry(entry.Name!, Trim(entry.Comment), entry.Words ?? "", entry.Span)),
        ];
    }

    /// <summary>
    /// Where <paramref name="data"/> is, spanning the element's own name. LINQ-to-XML reports one-based
    /// line and column, and a column pointing at the first character after the <c>&lt;</c>.
    /// </summary>
    private static LinePositionSpan Span(XElement data)
    {
        if (data is not IXmlLineInfo line || !line.HasLineInfo())
        {
            return default;
        }

        var start = new LinePosition(line.LineNumber - 1, line.LinePosition - 1);

        return new LinePositionSpan(start, new LinePosition(start.Line, start.Character + "data".Length));
    }

    /// <summary>A note is prose over one or more lines; the generated XmlDoc wants it without the resx's indent.</summary>
    private static string? Trim(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        var lines = comment!
            .Split('\n')
            .Select(line => line.Trim('\r', ' ', '\t'))
            .Where(line => line.Length > 0);

        return string.Join(" ", lines);
    }
}
