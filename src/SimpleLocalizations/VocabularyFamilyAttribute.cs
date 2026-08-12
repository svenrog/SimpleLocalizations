namespace SimpleLocalizations;

/// <summary>
/// Marks a set whose every member must have a key authored under <see cref="Prefix"/> (<c>SL1011</c>).
/// <para>
/// For the families a read edge composes from a member's own name — <c>category.{member}</c> for the heading
/// a group renders under. No producer ever names that key, so nothing notices one missing: the lookup falls
/// back to echoing the word it was handed, and the member renders as its own slug in every culture.
/// </para>
/// <para>
/// Checked where the set is declared, against the vocabularies that compilation authors — which is where the
/// words for it belong, since a member added there and a heading added elsewhere is the drift this catches.
/// </para>
/// <para>
/// Repeatable, because one set can be worded under more than one family — a band a score falls in is worded
/// once per thing scored. Each is a claim of its own, and each gets its own lookup.
/// </para>
/// </summary>
[AttributeUsage(
    AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class VocabularyFamilyAttribute : Attribute
{
    /// <summary>Declares that every member needs <c>{prefix}.{member-lowercased}</c> authored.</summary>
    public VocabularyFamilyAttribute(string prefix) => Prefix = prefix;

    /// <summary>The family the keys are authored under.</summary>
    public string Prefix { get; }
}
