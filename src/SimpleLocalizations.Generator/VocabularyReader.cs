using System.Xml.Linq;
using System.Xml;

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
            document = XDocument.Parse(resx);
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
                    Words: data.Element("value")?.Value))
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .Select(entry => new VocabularyEntry(entry.Name!, Trim(entry.Comment), entry.Words ?? "")),
        ];
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
