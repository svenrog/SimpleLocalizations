using Microsoft.CodeAnalysis;

namespace SimpleLocalizations.Generator;

/// <summary>What a vocabulary can be wrong about before anything is generated from it.</summary>
internal static class VocabularyDiagnostics
{
    private const string _category = "SimpleLocalizations";

    /// <summary>
    /// One spelling for every key. A key is persisted identity — it reaches a database, a serialized payload
    /// and whatever reads it back — so it is lowercase wherever it is authored, and the segments are what the
    /// generated members are named after.
    /// </summary>
    public static readonly DiagnosticDescriptor Malformed = new(
        "SL1001",
        "Vocabulary key is not lowercase",
        "'{0}' in {1} is not a lowercase key (dots separate families; digits and hyphens allowed)",
        _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// A key that is a proper prefix of another has no name left to take — the segment is the family the
    /// others nest under — and a family test would match it as one of its own members.
    /// </summary>
    public static readonly DiagnosticDescriptor Nested = new(
        "SL1002",
        "Vocabulary key is a prefix of another key",
        "'{0}' in {1} is a prefix of another key; give it a segment of its own",
        _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>A vocabulary states which key type it produces; there is no default worth guessing.</summary>
    public static readonly DiagnosticDescriptor Undeclared = new(
        "SL1003",
        "Vocabulary resource declares no key type",
        "{0} is marked as a vocabulary but sets no VocabularyKeyType",
        _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// A resource that cannot be read authors no key, and an empty vocabulary class moves the whole failure to
    /// the call sites: one error per member named, none of them naming the file that stopped holding them.
    /// </summary>
    public static readonly DiagnosticDescriptor Unreadable = new(
        "SL1004",
        "Vocabulary resource could not be read",
        "{0} is marked as a vocabulary but could not be read as a resource file",
        _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>The same silence from the other side: a readable resource with no key left in it.</summary>
    public static readonly DiagnosticDescriptor Empty = new(
        "SL1005",
        "Vocabulary resource authors no keys",
        "{0} is marked as a vocabulary but authors no key the generator can name",
        _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// A key type declaration is one default type, optionally followed by the families that differ from it.
    /// Without the default, every family outside the listed ones has no type to name and the generated member
    /// is emitted with the type missing — a syntax error in generated source, pointing at no resource item.
    /// </summary>
    public static readonly DiagnosticDescriptor KeyTypeMalformed = new(
        "SL1006",
        "Vocabulary key type declaration is not a default followed by families",
        "VocabularyKeyType on {0} ('{1}') is not one default type optionally followed by 'family=Type' entries separated by '|'",
        _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Two keys whose segments produce one member name. PascalCasing loses information — a digit has no case,
    /// so <c>tls-1-0</c> and <c>tls10</c> arrive as the same identifier — and the emitter would write a class
    /// with the member declared twice.
    /// </summary>
    public static readonly DiagnosticDescriptor Collides = new(
        "SL1007",
        "Two vocabulary keys produce one member name",
        "'{0}' in {1} produces the member name '{2}', which '{3}' already produces; spell one of the segments so they differ",
        _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// A segment naming the class it would be emitted into. C# forbids a member named after its enclosing
    /// type, so the family others nest under cannot also be one of them.
    /// </summary>
    public static readonly DiagnosticDescriptor Shadows = new(
        "SL1008",
        "Vocabulary key names the class it nests in",
        "'{0}' in {1} produces the member name '{2}', which is the class it nests in; give it a segment of its own",
        _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// A key type whose body cannot be written. Reported rather than left to the compiler, whose complaint
    /// would be a missing <c>From</c> at every call site instead of the one word that is absent.
    /// </summary>
    public static readonly DiagnosticDescriptor NotPartial = new(
        "SL1013",
        "A vocabulary key type is not partial",
        "'{0}' is marked [VocabularyKey] but is not partial; the generator writes its constructor, Key and From",
        _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
