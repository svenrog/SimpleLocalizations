using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Text;
using System.Xml;

namespace SimpleLocalizations.Generator;

/// <summary>Reads the authored keys out of a <c>.resx</c>, with the note each carries.</summary>
internal static class VocabularyReader
{
    /// <summary>
    /// Streamed rather than loaded as a document. A resource file is read once and nothing asks it a second
    /// question, so the tree an <c>XDocument</c> builds is an object graph over the whole file to be walked
    /// once and dropped — which a vocabulary of any size pays for in full before the first key is seen.
    /// </summary>
    private static readonly XmlReaderSettings _settings = new()
    {
        // Prohibited rather than ignored: this parses a file from a consumer's repository, and a document
        // type declaration is the one part of XML that can ask the parser to go and fetch something.
        DtdProcessing = DtdProcessing.Prohibit,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
        IgnoreWhitespace = true,
        CloseInput = true,
    };

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
        try
        {
            return Entries(resx);
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static IReadOnlyList<VocabularyEntry>? Entries(string resx)
    {
        using var reader = XmlReader.Create(new StringReader(resx), _settings);

        if (!Opens(reader))
        {
            return null;
        }

        var entries = new List<VocabularyEntry>();

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Depth != 1 || !Named(reader, "data"))
            {
                continue;
            }

            // Read where the element is before anything moves off it, because that is what a diagnostic
            // about this entry points at.
            var span = Span(reader);
            var name = reader.GetAttribute("name");

            if (reader.GetAttribute("type") is not null || reader.GetAttribute("mimetype") is not null)
            {
                // Skipped whole rather than read and discarded: the value of a serialized entry is a payload
                // nothing here has a use for.
                reader.Skip();
                continue;
            }

            var (value, comment) = Content(reader);

            if (!string.IsNullOrEmpty(name))
            {
                entries.Add(new VocabularyEntry(name!, Trim(comment), value, span));
            }
        }

        return entries;
    }

    /// <summary>Whether the document opens as a resx — its root element being the one thing that says so.</summary>
    private static bool Opens(XmlReader reader)
    {
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                return reader.LocalName == "root";
            }
        }

        return false;
    }

    /// <summary>
    /// The <c>value</c> and <c>comment</c> of the entry the reader is positioned on, leaving it on the
    /// element's end tag. Words are empty rather than absent when the entry authors none — the identity is
    /// the vocabulary's and the words are the data's.
    /// </summary>
    private static (string Value, string? Comment) Content(XmlReader reader)
    {
        var value = "";
        string? comment = null;

        if (reader.IsEmptyElement)
        {
            return (value, comment);
        }

        var depth = reader.Depth;

        while (!reader.EOF && !(reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth))
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Depth != depth + 1)
            {
                reader.Read();
                continue;
            }

            if (Named(reader, "value"))
            {
                value = Text(reader);
            }
            else if (Named(reader, "comment"))
            {
                comment = Text(reader);
            }
            else
            {
                reader.Skip();
                continue;
            }

            reader.Read();
        }

        return (value, comment);
    }

    /// <summary>
    /// The whole text of the element the reader is on, tags within it dropped and their text kept: a note is
    /// prose, and one written with markup in it says what its text says. Leaves the reader on the end tag.
    /// </summary>
    private static string Text(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            return "";
        }

        var depth = reader.Depth;
        var text = "";
        StringBuilder? more = null;

        while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth))
        {
            if (reader.NodeType is not (XmlNodeType.Text or XmlNodeType.CDATA
                or XmlNodeType.SignificantWhitespace or XmlNodeType.Whitespace))
            {
                continue;
            }

            // Nearly every entry is one text node, and holding the first rather than building on it keeps
            // the buffer for the ones that are not.
            if (more is not null)
            {
                more.Append(reader.Value);
            }
            else if (text.Length == 0)
            {
                text = reader.Value;
            }
            else
            {
                more = new StringBuilder(text).Append(reader.Value);
            }
        }

        return more?.ToString() ?? text;
    }

    /// <summary>
    /// Whether the current element is <paramref name="name"/> in no namespace, which is how a resx spells
    /// every element it declares.
    /// </summary>
    private static bool Named(XmlReader reader, string name) =>
        reader.LocalName == name && reader.NamespaceURI.Length == 0;

    /// <summary>
    /// Where the entry is, so a diagnostic about it can point there. The <c>data</c> element's own name
    /// rather than the key: an element is where a reader looks, and its position is what the parser reports
    /// without a guess about how the attribute was spaced.
    /// </summary>
    private static LinePositionSpan Span(XmlReader reader)
    {
        if (reader is not IXmlLineInfo line || !line.HasLineInfo())
        {
            return default;
        }

        // One-based line and column, the column being the first character after the '<'.
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
