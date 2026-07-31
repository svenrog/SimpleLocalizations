using System.Globalization;
using System.Resources;

namespace SimpleLocalizations.Tests;

/// <summary>
/// The catalog's four load-bearing behaviours, each guarding a failure that renders perfectly while being
/// wrong: a missing key, an unauthored one, stored text following the reader, and a culture file that is a
/// copy rather than an override list.
/// </summary>
[Collection(CultureCollection.Name)]
public class StringCatalogTests
{
    private static readonly TextCultures _cultures = new("en-US", "sv-SE");

    private static readonly StringCatalog _catalog =
        _cultures.Catalog("SimpleLocalizations.Tests.CatalogFixture", typeof(StringCatalogTests).Assembly);

    private static readonly CultureInfo _swedish = CultureInfo.GetCultureInfo("sv-SE");

    [Fact]
    public void A_key_absent_from_every_culture_throws()
    {
        // The one deviation from degrade-to-null: text rendering as its own key is how a half-translated
        // build reaches a reader unnoticed, and a missing resource is fixed at build time.
        Assert.Throws<MissingManifestResourceException>(() => _catalog.Get("no-such-key"));
    }

    [Fact]
    public void An_empty_value_reads_as_unauthored_rather_than_as_words()
    {
        Assert.False(_catalog.TryGet("data-titled", CultureInfo.InvariantCulture, out var value));
        Assert.Equal("", value);
    }

    [Fact]
    public void A_culture_renders_its_own_words()
    {
        Assert.Equal("Hej", _catalog.Get("greeting", _swedish));
        Assert.Equal("Hello", _catalog.Get("greeting", _cultures.NeutralCulture));
    }

    [Fact]
    public void A_key_a_culture_does_not_override_falls_back_to_the_neutral_set()
    {
        Assert.Equal("HTTP", _catalog.Get("unchanged", _swedish));
    }

    [Fact]
    public void Stored_text_does_not_move_with_the_reader()
    {
        var before = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = _swedish;

            Assert.Equal("hittade 1 av 3", _catalog.Get("composed", _swedish).Replace("{0}", "1").Replace("{1}", "3"));
            Assert.Equal("found 1 of 3", _catalog.Neutral("composed", "1", "3"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }

    [Fact]
    public void A_culture_file_authors_no_key_the_neutral_set_lacks()
    {
        // A key only a culture has is unreachable: nothing resolves it, because nothing declared it.
        var neutral = _catalog.AuthoredKeys(_cultures.NeutralCulture);
        var swedish = _catalog.AuthoredKeys(_swedish);

        Assert.NotEmpty(neutral);
        Assert.NotEmpty(swedish);
        Assert.Empty(swedish.Except(neutral));
    }

    [Fact]
    public void A_culture_overrides_only_what_it_spells_differently()
    {
        Assert.DoesNotContain("unchanged", _catalog.AuthoredKeys(_swedish));
    }

    [Fact]
    public void Text_from_the_catalog_carries_the_proof_that_it_came_from_one()
    {
        // LocalizedText's constructor is private and its one factory takes a catalog and a key, so a call
        // site cannot word a sentence itself.
        var said = _catalog.Say("greeting");

        Assert.Equal(_catalog.Get("greeting"), said.Text);
        Assert.Equal(said.Text, said.ToString());
    }
}
