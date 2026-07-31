using System.Globalization;

namespace SimpleLocalizations.Tests;

/// <summary>
/// The reason a list join cannot be <c>string.Join(", ")</c>: the same call has to produce different
/// punctuation for different readers.
/// </summary>
[Collection(CultureCollection.Name)]
public class ListFormatterTests
{
    private static T InCulture<T>(string culture, Func<T> read)
    {
        var before = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            return read();
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public void The_serial_comma_divides_the_two_english_cultures()
    {
        Assert.Equal("a, b, and c", InCulture("en-US", () => ListFormatter.And(["a", "b", "c"])));
        Assert.Equal("a, b and c", InCulture("en-GB", () => ListFormatter.And(["a", "b", "c"])));
    }

    [Fact]
    public void A_culture_rewrites_the_conjunction_rather_than_respelling_it()
    {
        Assert.Equal("a, b och c", InCulture("sv-SE", () => ListFormatter.And(["a", "b", "c"])));
        Assert.Equal("a eller b", InCulture("sv-SE", () => ListFormatter.Or(["a", "b"])));
    }

    [Theory]
    [InlineData(new string[0], "")]
    [InlineData(new[] { "a" }, "a")]
    [InlineData(new[] { "a", "b" }, "a and b")]
    [InlineData(new[] { "a", "b", "c", "d" }, "a, b, c, and d")]
    public void A_list_of_any_length_reads_as_prose(string[] items, string expected)
    {
        Assert.Equal(expected, InCulture("en-US", () => ListFormatter.And(items)));
    }

    [Fact]
    public void A_blank_item_is_not_a_member()
    {
        Assert.Equal("a and b", InCulture("en-US", () => ListFormatter.And(["a", "  ", "b"])));
    }

    [Fact]
    public void Truncation_counts_what_it_dropped_and_stays_a_plain_join()
    {
        Assert.Equal("a, b", InCulture("en-US", () => ListFormatter.Truncated(["a", "b"], 3)));
        Assert.Equal("a, b, +2 more", InCulture("en-US", () => ListFormatter.Truncated(["a", "b", "c", "d"], 2)));
        Assert.Equal("a, b, +2 till", InCulture("sv-SE", () => ListFormatter.Truncated(["a", "b", "c", "d"], 2)));
    }
}
