namespace SimpleLocalizations;

/// <summary>
/// Marks a key type: the generator writes its body, and its <c>From</c> factory belongs to the generated
/// vocabulary alone.
/// <para>
/// The type is declared <see langword="partial"/> and empty — <c>[VocabularyKey] public readonly partial
/// record struct FindingKey;</c> — and the generator supplies the private constructor, the <c>Key</c>
/// property and the factory. Written rather than hand-rolled because the shape is the rule: a public
/// constructor anywhere would make the vocabulary open again.
/// </para>
/// <para>
/// The factory has to be public: the declaration is generated into whichever project authors the resource,
/// which is rarely the one declaring the key type. Public and unguarded would mean any call site could invent
/// a key, and a key authored nowhere resolves to no text and reads as a finished record — so the guard is a
/// compile-time rule (<c>SL1010</c>) rather than an accessibility modifier.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class VocabularyKeyAttribute : Attribute
{
    /// <summary>
    /// The type whose constant members name the families a key of this type may be filed under, or
    /// <see langword="null"/> when it may be filed under any (<c>SL1012</c>).
    /// <para>
    /// Declared here rather than in the build, because it is a claim about this type: where a record is filed
    /// under its key's first segment and carries no grouping of its own, a key authored outside the set names
    /// a grouping nothing can produce.
    /// </para>
    /// </summary>
    public Type? Families { get; set; }
}
