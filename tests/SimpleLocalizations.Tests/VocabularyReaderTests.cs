using SimpleLocalizations.Generator;

namespace SimpleLocalizations.Tests;

/// <summary>
/// What a <c>.resx</c> says, as the component reads it. Asserted here rather than through the generator
/// because most of this is invisible from there: a binary entry and an absent one produce the same nothing,
/// and the span an entry carries is what every diagnostic about it points at.
/// </summary>
public class VocabularyReaderTests
{
    private static string Resource(string body) =>
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
        {body}
        </root>
        """;

    [Fact]
    public void An_entry_carries_its_key_its_words_and_its_note()
    {
        var entries = VocabularyReader.Read(Resource(
            """
              <data name="alpha.one" xml:space="preserve">
                <value>One</value>
                <comment>Why this entry exists.</comment>
              </data>
            """))!;

        var entry = Assert.Single(entries);

        Assert.Equal("alpha.one", entry.Key);
        Assert.Equal("One", entry.Value);
        Assert.Equal("Why this entry exists.", entry.Comment);
    }

    [Fact]
    public void An_entry_points_at_its_own_data_element()
    {
        // Every diagnostic about a key is placed by this, and a span that is off by a line sends a reader to
        // the wrong entry in a file that may author hundreds.
        var entries = VocabularyReader.Read(Resource(
            """
              <data name="alpha.one"><value>One</value></data>
              <data name="alpha.two"><value>Two</value></data>
            """))!;

        Assert.Equal(3, entries[0].Span.Start.Line);
        Assert.Equal(4, entries[1].Span.Start.Line);
        Assert.Equal(3, entries[0].Span.Start.Character);
        Assert.Equal(7, entries[0].Span.End.Character);
        Assert.Equal(3, entries[0].Span.End.Line);
    }

    [Fact]
    public void An_entry_that_is_not_text_is_not_a_key()
    {
        // A resx holds files and serialized objects too, and a vocabulary is only ever text.
        var entries = VocabularyReader.Read(Resource(
            """
              <data name="icon" type="System.Drawing.Bitmap, System.Drawing"><value>icon.png</value></data>
              <data name="blob" mimetype="application/x-microsoft.net.object.binary.base64"><value>AAA=</value></data>
              <data name="alpha.one"><value>One</value></data>
            """))!;

        Assert.Equal(["alpha.one"], entries.Select(entry => entry.Key));
    }

    [Fact]
    public void A_note_over_several_lines_arrives_as_one()
    {
        // It becomes a single XmlDoc summary line, so the resx's own wrapping and indent are not the note.
        var entries = VocabularyReader.Read(Resource(
            """
              <data name="alpha.one" xml:space="preserve">
                <value>One</value>
                <comment>
                  What this is for,
                  over two lines.
                </comment>
              </data>
            """))!;

        Assert.Equal("What this is for, over two lines.", Assert.Single(entries).Comment);
    }

    [Fact]
    public void A_note_of_nothing_is_no_note()
    {
        var entries = VocabularyReader.Read(Resource(
            """
              <data name="alpha.one" xml:space="preserve"><value>One</value><comment>   </comment></data>
            """))!;

        Assert.Null(Assert.Single(entries).Comment);
    }

    [Fact]
    public void An_entry_authoring_no_words_reads_as_empty_rather_than_absent()
    {
        // The identity is the vocabulary's and the words are the data's; the key exists, the value does not.
        var entries = VocabularyReader.Read(Resource("""  <data name="alpha.one" />"""))!;

        Assert.Equal("", Assert.Single(entries).Value);
    }

    [Fact]
    public void An_entry_with_no_name_is_not_a_key()
    {
        var entries = VocabularyReader.Read(Resource(
            """
              <data><value>One</value></data>
              <data name=""><value>Two</value></data>
            """))!;

        Assert.Empty(entries);
    }

    [Fact]
    public void A_resource_authoring_nothing_is_read_as_empty_and_not_as_unreadable()
    {
        // The distinction SL1004 and SL1005 rest on: a file nothing can read is a different mistake from one
        // that reads and declares no key.
        Assert.Empty(VocabularyReader.Read(Resource(""))!);
    }

    [Theory]
    [InlineData("<not-a-resx/>")]
    [InlineData("<root><data name=\"alpha.one\"></root>")]
    [InlineData("")]
    [InlineData("plain words")]
    public void Anything_that_is_not_a_resource_reads_as_nothing_at_all(string resx)
    {
        Assert.Null(VocabularyReader.Read(resx));
    }
}
