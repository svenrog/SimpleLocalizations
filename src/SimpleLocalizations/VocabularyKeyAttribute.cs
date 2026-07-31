namespace SimpleLocalizations;

/// <summary>
/// Marks a key type whose <c>From</c> factory belongs to the generated vocabulary alone.
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
}
