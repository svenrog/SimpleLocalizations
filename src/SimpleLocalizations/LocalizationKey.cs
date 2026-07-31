namespace SimpleLocalizations;

/// <summary>
/// The key type a vocabulary produces when it declares none — text a surface resolves and renders, keyed.
/// <para>
/// One kind of key is the common case, and declaring a type to say so is ceremony. A project that keys more
/// than one kind of thing declares its own and names them in <c>VocabularyKeyType</c>, because a key of one
/// kind standing in for another resolves to nothing and falls back without a word of complaint. Until then
/// this is the default.
/// </para>
/// <para>
/// Written out rather than generated, because it is this package's own and nothing generates into here.
/// </para>
/// </summary>
[VocabularyKey]
public readonly record struct LocalizationKey
{
    private LocalizationKey(string key) => Key = key;

    /// <summary>The key the catalog resolves.</summary>
    public string Key { get; }

    /// <summary>The generated declaration's constructor.</summary>
    public static LocalizationKey From(string key) => new(key);

    /// <inheritdoc />
    public override string ToString() => Key ?? "";
}
