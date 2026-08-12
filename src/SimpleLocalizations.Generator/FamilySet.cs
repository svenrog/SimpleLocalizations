using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace SimpleLocalizations.Generator;

/// <summary>
/// A <c>[VocabularyFamily]</c> enum as the generator reads it: the family its members are worded under, and
/// the word each member is spelled by. What the emitter turns into the lookup from a member to its key.
/// <para>
/// Enums only. The lookup is a <see langword="switch"/> over the set, and the other shapes a closed set takes
/// — a constant, a static readonly instance — are values a caller already holds the key spelling of.
/// </para>
/// <para>
/// Carried as strings so the pipeline can tell one run's inputs from the last's; a symbol compares by
/// reference across compilations and would re-emit every vocabulary on every keystroke.
/// </para>
/// </summary>
internal readonly struct FamilySet : IEquatable<FamilySet>
{
    private const string _marker = "SimpleLocalizations.VocabularyFamilyAttribute";

    private FamilySet(string prefix, string type, bool exposed, ImmutableArray<FamilyMember> members)
    {
        Prefix = prefix;
        Type = type;
        Exposed = exposed;
        Members = members;
    }

    /// <summary>The family the words are authored under, which is the key path the lookup is emitted onto.</summary>
    public string Prefix { get; }

    /// <summary>The set's <c>global::</c>-qualified name, as the emitted signature and its arms spell it.</summary>
    public string Type { get; }

    /// <summary>
    /// Whether the set is visible outside its assembly. A generated class is public, and a public method
    /// taking an internal parameter does not compile — so an unexposed set gets an internal lookup.
    /// </summary>
    public bool Exposed { get; }

    /// <summary>The members, with the word each is worded by, in declaration order.</summary>
    public ImmutableArray<FamilyMember> Members { get; }

    /// <summary>
    /// One set per family <paramref name="context"/>'s target words, the attribute being repeatable. Empty
    /// where it states none. Enum-ness is the predicate's to say, so what reaches here is one or is
    /// unresolvable.
    /// </summary>
    public static ImmutableArray<FamilySet> Read(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol set)
        {
            return ImmutableArray<FamilySet>.Empty;
        }

        // Read once: a set's members, its name and its reach are the same under every family it words.
        var members = Spelled(set);
        var type = set.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var exposed = Exposes(set);
        var families = ImmutableArray.CreateBuilder<FamilySet>();

        foreach (var attribute in context.Attributes)
        {
            if (attribute.AttributeClass?.ToDisplayString() == _marker
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is string prefix
                && prefix.Length > 0)
            {
                families.Add(new FamilySet(prefix, type, exposed, members));
            }
        }

        return families.ToImmutable();
    }

    /// <summary>The set's members, with the word each is worded by, as <c>SL1011</c> spells them.</summary>
    private static ImmutableArray<FamilyMember> Spelled(INamedTypeSymbol set)
    {
        var members = ImmutableArray.CreateBuilder<FamilyMember>();

        foreach (var (name, word, _) in VocabularyKeys.Members(set))
        {
            members.Add(new FamilyMember(name, word));
        }

        return members.ToImmutable();
    }

    /// <summary>
    /// Whether nothing enclosing <paramref name="set"/> narrows it below public. Asked of the whole chain: a
    /// public enum nested in an internal class is internal, and the emitted signature has to say so.
    /// </summary>
    private static bool Exposes(INamedTypeSymbol set)
    {
        for (var type = set; type is not null; type = type.ContainingType)
        {
            if (type.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    public bool Equals(FamilySet other) =>
        Prefix == other.Prefix
        && Type == other.Type
        && Exposed == other.Exposed
        && Members.SequenceEqual(other.Members);

    public override bool Equals(object? obj) => obj is FamilySet other && Equals(other);

    public override int GetHashCode() => (Prefix, Type, Exposed, Members.Length).GetHashCode();
}
