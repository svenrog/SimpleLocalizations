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
    private readonly string? _key;

    private LocalizationKey(string key) => _key = key;

    /// <summary>
    /// The key the catalog resolves. Computed rather than stored, because a struct is always
    /// <see langword="default"/>-constructible and the signature promises no null.
    /// </summary>
    public string Key => _key ?? "";

    /// <summary>The generated declaration's constructor.</summary>
    public static LocalizationKey From(string key) => new(key);

    /// <inheritdoc />
    public override string ToString() => Key;
}
