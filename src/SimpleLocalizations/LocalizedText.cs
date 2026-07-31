namespace SimpleLocalizations;

/// <summary>
/// Text that came out of a resource set, as a value a string literal cannot become.
/// <para>
/// For the surfaces that render straight into the reader's culture and are read once — a refusal, an error
/// body — rather than the ones that store a key and resolve it at a read edge. Those need a key of their own;
/// this only needs a marker saying the sentence was authored somewhere a translator can reach, so a call site
/// cannot hand a refusal its own English.
/// </para>
/// <para>
/// The constructor is <b>private</b> and the one factory takes a <see cref="StringCatalog"/> and a key, so
/// the marker cannot be forged even from inside this assembly: there is no path here that does not start at
/// a resource file.
/// </para>
/// </summary>
public readonly struct LocalizedText : IEquatable<LocalizedText>
{
    private LocalizedText(string text) => Text = text;

    /// <summary>The rendered sentence, in the ambient UI culture as of when it was produced.</summary>
    public string Text { get; }

    /// <summary>
    /// The only producer: <paramref name="key"/> resolved against <paramref name="catalog"/>, with
    /// <paramref name="args"/> filled in. Reached through <see cref="StringCatalog.Say(string, object?[])"/>.
    /// </summary>
    internal static LocalizedText From(StringCatalog catalog, string key, object?[] args) =>
        new(args.Length == 0 ? catalog.Get(key) : catalog.Format(key, args));

    public static bool operator ==(LocalizedText left, LocalizedText right) => left.Equals(right);

    public static bool operator !=(LocalizedText left, LocalizedText right) => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(LocalizedText other) => string.Equals(Text, other.Text, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is LocalizedText other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Text is null ? 0 : StringComparer.Ordinal.GetHashCode(Text);

    /// <summary>The sentence — so an interpolation of one reads as it always did.</summary>
    public override string ToString() => Text ?? "";
}
