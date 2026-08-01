using static SimpleLocalizations.Tests.VocabularyHarness;

namespace SimpleLocalizations.Tests;

/// <summary>
/// What the generator emits, and what it refuses before emitting anything. Every refusal here is a failure
/// that would otherwise arrive as a pile of <c>CS</c> errors at call sites naming no resource file.
/// </summary>
public class VocabularyGeneratorTests
{
    private static Declaration Declared(string keyType = "Probe.ProbeKey", string derived = "") =>
        new("ProbeKeys", keyType, "Probe", derived);

    [Fact]
    public void Emits_a_member_per_key_under_a_class_per_family()
    {
        var run = Generate(Keys("alpha.one", "alpha.two", "beta.deep.three"), Declared());

        Assert.Empty(run.Ids);
        Assert.Contains("public static class ProbeKeys", run.OnlySource);
        Assert.Contains("public static class Alpha", run.OnlySource);
        Assert.Contains("public static global::Probe.ProbeKey One =>", run.OnlySource);
        Assert.Contains("global::Probe.ProbeKey.From(\"alpha.one\")", run.OnlySource);
        Assert.Contains("public static class Deep", run.OnlySource);
        Assert.Contains("global::Probe.ProbeKey.From(\"beta.deep.three\")", run.OnlySource);
    }

    [Fact]
    public void A_family_carries_its_own_prefix_and_membership_test()
    {
        var run = Generate(Keys("alpha.one"), Declared());

        Assert.Contains("public const string Prefix = \"alpha.\";", run.OnlySource);
        Assert.Contains("public static bool Covers(string key) =>", run.OnlySource);
    }

    [Fact]
    public void A_segment_is_pascal_cased_part_by_part()
    {
        var run = Generate(Keys("alpha.not-http-only"), Declared());

        Assert.Contains("NotHttpOnly =>", run.OnlySource);
    }

    [Fact]
    public void A_segment_starting_with_a_digit_takes_an_underscore()
    {
        var run = Generate(Keys("alpha.1password"), Declared());

        Assert.Contains("_1password =>", run.OnlySource);
    }

    [Fact]
    public void A_note_beside_a_key_becomes_the_members_documentation()
    {
        var run = Generate(Resx(("alpha.one", "One", "Why this entry exists.")), Declared());

        Assert.Contains("/// <summary>Why this entry exists.</summary>", run.OnlySource);
    }

    [Fact]
    public void A_family_names_the_key_type_declared_for_it_over_the_default()
    {
        var run = Generate(
            Keys("alpha.one", "detail.two"),
            Declared("Probe.ProbeKey | detail=Probe.OtherKey"));

        Assert.Contains("global::Probe.ProbeKey One =>", run.OnlySource);
        Assert.Contains("global::Probe.OtherKey Two =>", run.OnlySource);
    }

    [Fact]
    public void A_declaration_separated_by_a_semicolon_arrives_truncated()
    {
        // Why SL1009 exists, and why it cannot be a Roslyn diagnostic: the metadata reaches here through an
        // .editorconfig, whose parser reads ';' as the start of a comment. Everything after the first entry
        // is gone before the generator is handed anything, so the wrong key types are generated and the
        // build is clean. By the time this component runs there is nothing left to report.
        var run = Generate(
            Keys("alpha.one", "detail.two"),
            Declared("Probe.ProbeKey ; detail=Probe.OtherKey"));

        Assert.Empty(run.Ids);
        Assert.DoesNotContain("OtherKey", run.OnlySource);
        Assert.Contains("global::Probe.ProbeKey Two =>", run.OnlySource);
    }

    [Fact]
    public void A_derived_suffix_is_authored_but_generates_no_member()
    {
        var run = Generate(Keys("alpha.one", "alpha.one.pitch"), Declared(derived: "pitch"));

        Assert.Empty(run.Ids);
        Assert.Contains("One =>", run.OnlySource);
        Assert.DoesNotContain("Pitch", run.OnlySource);
    }

    [Fact]
    public void A_resource_authoring_no_words_still_declares_its_key()
    {
        // The identity is the vocabulary's and the words are the data's. The member exists; the value does not.
        var run = Generate(Resx(("alpha.one", "", null)), Declared());

        Assert.Empty(run.Ids);
        Assert.Contains("One =>", run.OnlySource);
    }

    [Theory]
    [InlineData("Alpha.One")]
    [InlineData("alpha.One")]
    [InlineData("alpha..one")]
    [InlineData("alpha.one ")]
    [InlineData("alpha one")]
    [InlineData("alpha_one")]
    public void SL1001_refuses_a_key_that_is_not_lowercase(string key)
    {
        Assert.Equal(["SL1001"], Generate(Keys(key), Declared()).Ids);
    }

    [Fact]
    public void A_resource_name_becomes_a_catalog_factory_on_the_class()
    {
        // The one string a call site would otherwise spell by hand and cannot check: a base name that does
        // not match what the assembly embedded resolves nothing and throws on first use.
        var run = Generate(
            Keys("alpha.one"),
            Declared() with { ResourceName = "MyApp.Localization.Strings" });

        Assert.Empty(run.Ids);
        Assert.Contains("public const string ResourceName = \"MyApp.Localization.Strings\";", run.OnlySource);
        Assert.Contains("cultures.Catalog(ResourceName, typeof(ProbeKeys).Assembly);", run.OnlySource);
    }

    [Fact]
    public void A_resource_naming_no_set_gets_no_factory()
    {
        var run = Generate(Keys("alpha.one"), Declared());

        Assert.DoesNotContain("ResourceName", run.OnlySource);
        Assert.DoesNotContain("Catalog", run.OnlySource);
    }

    [Theory]
    [InlineData("catalog", "CatalogKey")]
    [InlineData("resource-name", "ResourceNameKey")]
    [InlineData("prefix", "PrefixKey")]
    [InlineData("covers", "CoversKey")]
    public void A_key_naming_a_generated_member_takes_a_suffix(string key, string member)
    {
        // The classes carry these themselves, so a segment spelled the same would be declared twice.
        var run = Generate(Keys(key), Declared());

        Assert.Empty(run.Ids);
        Assert.Contains($"{member} =>", run.OnlySource);
    }

    [Fact]
    public void A_flat_resource_needs_no_family_at_all()
    {
        // A family is what a dot buys, not something every vocabulary owes: console furniture, a set of
        // refusals, anything nothing groups.
        var run = Generate(Keys("greeting", "farewell"), Declared());

        Assert.Empty(run.Ids);
        Assert.Contains("public static global::Probe.ProbeKey Greeting =>", run.OnlySource);
        Assert.Contains("global::Probe.ProbeKey.From(\"farewell\")", run.OnlySource);
        Assert.DoesNotContain("Prefix", run.OnlySource);
    }

    [Fact]
    public void A_flat_key_and_a_family_can_share_one_resource()
    {
        var run = Generate(Keys("greeting", "alpha.one"), Declared());

        Assert.Empty(run.Ids);
        Assert.Contains("Greeting =>", run.OnlySource);
        Assert.Contains("public static class Alpha", run.OnlySource);
    }

    [Fact]
    public void A_flat_key_is_still_refused_where_it_is_a_prefix_of_another()
    {
        Assert.Equal(["SL1002"], Generate(Keys("alpha", "alpha.one"), Declared()).Ids);
    }

    [Fact]
    public void SL1002_refuses_a_key_that_is_a_prefix_of_another()
    {
        Assert.Equal(["SL1002"], Generate(Keys("alpha.one", "alpha.one.two"), Declared()).Ids);
    }

    [Fact]
    public void SL1003_refuses_a_vocabulary_declaring_no_key_type()
    {
        Assert.Equal(["SL1003"], Generate(Keys("alpha.one"), Declared(keyType: "")).Ids);
    }

    [Fact]
    public void SL1004_refuses_a_resource_nothing_can_read()
    {
        Assert.Equal(["SL1004"], Generate("<not-a-resx/>", Declared()).Ids);
    }

    [Fact]
    public void SL1005_refuses_a_readable_resource_authoring_no_key()
    {
        Assert.Equal(["SL1005"], Generate(Keys(), Declared()).Ids);
    }

    [Theory]
    [InlineData("detail=Probe.OtherKey")]
    [InlineData("Probe.ProbeKey | detail")]
    [InlineData("Probe.ProbeKey | a=b=c")]
    [InlineData("Probe.ProbeKey | detail=Probe.OtherKey | detail=Probe.ProbeKey")]
    public void SL1006_refuses_a_key_type_that_is_not_a_default_followed_by_families(string declaration)
    {
        Assert.Equal(["SL1006"], Generate(Keys("alpha.one"), Declared(declaration)).Ids);
    }

    [Fact]
    public void SL1007_refuses_two_keys_producing_one_member_name()
    {
        // PascalCasing loses information: a digit has no case, so both segments arrive as Tls10.
        Assert.Equal(["SL1007"], Generate(Keys("alpha.tls-1-0", "alpha.tls10"), Declared()).Ids);
    }

    [Fact]
    public void SL1008_refuses_a_segment_naming_the_class_it_nests_in()
    {
        Assert.Equal(["SL1008"], Generate(Keys("alpha.alpha.one"), Declared()).Ids);
    }

    [Fact]
    public void A_refusal_points_at_the_entry_that_caused_it()
    {
        // A diagnostic carrying no location cannot be navigated to and cannot be suppressed, and leaves a
        // reader to find the key in a file that may author hundreds.
        var run = Generate(Keys("alpha.one", "alpha.two", "Bad.Key"), Declared());

        var location = run.Diagnostics.Single().Location;
        var lines = location.GetLineSpan();

        Assert.Equal("Vocab.resx", lines.Path);
        Assert.Equal(Keys("alpha.one", "alpha.two", "Bad.Key").Split('\n')
            .Select((line, index) => (line, index))
            .Single(pair => pair.line.Contains("Bad.Key")).index,
            lines.StartLinePosition.Line);
    }

    [Fact]
    public void A_refusal_about_the_file_points_at_the_file()
    {
        var run = Generate(Keys(), Declared());

        Assert.Equal("Vocab.resx", run.Diagnostics.Single().Location.GetLineSpan().Path);
    }

    [Theory]
    [InlineData("Bad.Key")]
    [InlineData("alpha.one.two")]
    [InlineData("alpha.tls-1-0")]
    public void A_key_that_cannot_be_emitted_costs_itself_and_not_the_file(string refused)
    {
        // Refusing the whole vocabulary over one bad key moves the failure to every *other* key's call sites,
        // as a pile of CS0117 naming no resource file — the cascade SL1004 and SL1005 exist to prevent.
        var run = Generate(Keys("alpha.one", "alpha.tls10", refused), Declared());

        Assert.NotEmpty(run.Ids);
        Assert.Contains("One =>", run.OnlySource);
        Assert.Contains("Tls10 =>", run.OnlySource);
    }

    [Fact]
    public void A_vocabulary_whose_every_key_was_refused_does_not_also_report_being_empty()
    {
        // The refusals said why. SL1005 would only say that they did.
        Assert.Equal(["SL1001"], Generate(Keys("Bad.Key"), Declared()).Ids);
    }

    [Fact]
    public void A_resource_carrying_no_declaration_is_not_a_vocabulary()
    {
        // Most AdditionalFiles are not vocabularies, so an undeclared one produces nothing and reports nothing.
        var run = Generate([("Notes.md", "# not a resource", new Declaration("", "", ""))]);

        Assert.Empty(run.Ids);
        Assert.Empty(run.Sources);
    }

    [Fact]
    public void Each_resource_is_emitted_under_its_own_file_name()
    {
        // Two resources can carry one class name — a glob catching a culture file is the way in — and a
        // repeated hint name crashes the generator rather than reporting anything.
        var run = Generate(
        [
            ("Vocab.resx", Keys("alpha.one"), Declared()),
            ("Vocab.sv-SE.resx", Keys("alpha.one"), Declared()),
        ]);

        Assert.Empty(run.Ids);
        Assert.Equal(["Vocab.g.cs", "Vocab.sv-SE.g.cs"], run.Sources.Keys.Order());
    }

    [Fact]
    public void Two_resources_sharing_a_file_name_are_emitted_under_their_folders()
    {
        // A file name is not unique — `Localization/Strings.resx` beside `Shared/Strings.resx` is an ordinary
        // layout — and a repeated hint name throws inside the generator, which costs every vocabulary in the
        // project rather than the colliding pair. The build declares the name; this is what it declares.
        var run = Generate(
        [
            ("A/Strings.resx", Keys("alpha.one"),
                new Declaration("StringsKeys", "Probe.ProbeKey", "Probe.A", Hint: "A.Strings")),
            ("B/Strings.resx", Keys("beta.two"),
                new Declaration("StringsKeys", "Probe.ProbeKey", "Probe.B", Hint: "B.Strings")),
        ]);

        Assert.Empty(run.Ids);
        Assert.Equal(["A.Strings.g.cs", "B.Strings.g.cs"], run.Sources.Keys.Order());
    }

    [Fact]
    public void A_resource_whose_build_declares_no_hint_falls_back_to_its_path()
    {
        // Nothing in a build reaches here without the metadata, but the generator is handed AdditionalFiles
        // and must not throw on the one input it cannot check.
        var run = Generate(
        [
            ("A/Strings.resx", Keys("alpha.one"), new Declaration("StringsKeys", "Probe.ProbeKey", "Probe.A")),
            ("B/Strings.resx", Keys("beta.two"), new Declaration("StringsKeys", "Probe.ProbeKey", "Probe.B")),
        ]);

        Assert.Empty(run.Ids);
        Assert.Equal(["A.Strings.g.cs", "B.Strings.g.cs"], run.Sources.Keys.Order());
    }
}
