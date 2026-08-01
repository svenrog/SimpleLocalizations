using System.Globalization;

namespace SimpleLocalizations.Tests;

[Collection(CultureCollection.Name)]
public class TextCulturesTests
{
    private static readonly TextCultures _cultures = new("en-US", "en-GB", "sv-SE");

    [Fact]
    public void The_neutral_culture_leads_the_supported_set()
    {
        Assert.Equal(["en-US", "en-GB", "sv-SE"], _cultures.Supported);
    }

    [Fact]
    public void A_culture_named_twice_is_carried_once()
    {
        Assert.Equal(["en-US", "en-GB"], new TextCultures("en-US", "en-GB", "en-us").Supported);
    }

    [Fact]
    public void An_unsupported_request_fails_rather_than_falling_back()
    {
        // An operator who asked for one language must not silently get another.
        Assert.False(_cultures.TryResolve("de-DE", out var culture));
        Assert.Equal("en-US", culture.Name);
    }

    [Theory]
    [InlineData("sv-SE", "sv-SE")]
    [InlineData("SV-se", "sv-SE")]
    [InlineData(null, "en-US")]
    [InlineData("  ", "en-US")]
    public void A_supported_request_resolves(string? requested, string expected)
    {
        Assert.True(_cultures.TryResolve(requested, out var culture));
        Assert.Equal(expected, culture.Name);
    }

    [Fact]
    public void A_preference_list_falls_back_where_a_request_would_fail()
    {
        // Accept-Language is wishes, not an instruction.
        Assert.Equal("en-US", _cultures.Negotiate(["de-DE", "fr-FR"]).Name);
        Assert.Equal("sv-SE", _cultures.Negotiate(["de-DE", "sv-SE;q=0.8"]).Name);
        Assert.Equal("en-US", _cultures.Negotiate(null).Name);
    }

    [Theory]
    [InlineData("sv", "sv-SE")]
    [InlineData("SV", "sv-SE")]
    [InlineData("sv-FI", "sv-SE")]
    [InlineData("en", "en-US")]
    [InlineData("en-AU", "en-US")]
    public void A_preference_reaches_a_culture_sharing_its_language(string preference, string expected)
    {
        // A browser sends the bare language more often than not. Refusing it answered a reader who asked for
        // Swedish in English, while holding Swedish.
        Assert.Equal(expected, _cultures.Negotiate([preference]).Name);
    }

    [Fact]
    public void A_near_first_preference_outranks_an_exact_second()
    {
        // The order is the reader's ranking, so each preference is tried both ways before the next is tried
        // at all: sv-SE is what "Swedish, or failing that British English" asked for.
        Assert.Equal("sv-SE", _cultures.Negotiate(["sv", "en-GB"]).Name);
    }

    [Fact]
    public void An_explicit_request_still_refuses_what_it_was_not_given()
    {
        // Negotiate reads wishes; TryResolve reads an instruction, and stays exact.
        Assert.False(_cultures.TryResolve("sv", out _));
    }

    [Fact]
    public void Applying_a_culture_leaves_the_formatting_axis_alone()
    {
        var formatting = CultureInfo.DefaultThreadCurrentCulture;
        var display = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            TextCultures.Apply(CultureInfo.GetCultureInfo("sv-SE"));

            Assert.Equal("sv-SE", CultureInfo.DefaultThreadCurrentUICulture?.Name);
            Assert.Equal(formatting, CultureInfo.DefaultThreadCurrentCulture);
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentUICulture = display;
        }
    }
}
