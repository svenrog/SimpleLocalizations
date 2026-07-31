using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SimpleLocalizations.Generator;

/// <summary>What both family rules need to read: the keys a project authors, and the members of a set.</summary>
internal static class VocabularyKeys
{
    /// <summary>
    /// Every key the project's vocabularies author. Read off the resources rather than the generated members,
    /// since a family a read edge composes at run time is what these rules are about — no member exists for it.
    /// </summary>
    public static ImmutableHashSet<string> Authored(AnalyzerOptions options, CancellationToken token)
    {
        var authored = ImmutableHashSet.CreateBuilder(StringComparer.Ordinal);

        foreach (var file in options.AdditionalFiles)
        {
            if (!VocabularyMetadata.IsVocabulary(options.AnalyzerConfigOptionsProvider, file))
            {
                continue;
            }

            foreach (var entry in VocabularyReader.Read(file.GetText(token)?.ToString() ?? "") ?? [])
            {
                authored.Add(entry.Key);
            }
        }

        return authored.ToImmutable();
    }

    /// <summary>
    /// The members of <paramref name="type"/>, with the word each is spelled by.
    /// <para>
    /// A closed set is written three ways and all three count: an <b>enum</b> member; a <b>constant</b>, whose
    /// value is read rather than its name, because a value is why it is a constant and not an enum
    /// (<c>Https = "http-s"</c> is spelled <c>http-s</c>); and a <b>static readonly field or property of the
    /// declaring type</b> — the type-safe enum a class reaches for when a member needs behaviour, which has no
    /// value to read and so is spelled by its name.
    /// </para>
    /// <para>
    /// Lowercased whichever it is, because a key is lowercase wherever it is authored.
    /// </para>
    /// </summary>
    public static IEnumerable<(string Name, string Word, ImmutableArray<Location> Locations)> Members(
        INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            var word = Spelling(member, type);

            if (word is not null)
            {
                yield return (member.Name, word.ToLowerInvariant(), member.Locations);
            }
        }
    }

    /// <summary>The word <paramref name="member"/> is spelled by, or <c>null</c> when it is not a member.</summary>
    private static string? Spelling(ISymbol member, INamedTypeSymbol declaring) =>
        member switch
        {
            IFieldSymbol { HasConstantValue: true, ConstantValue: string value } when value.Length > 0 => value,
            IFieldSymbol { HasConstantValue: true } field => field.Name,
            // AssociatedSymbol excludes an auto-property's backing field, which is a static readonly field of
            // the declaring type and would otherwise count its property twice.
            IFieldSymbol { IsStatic: true, IsReadOnly: true, AssociatedSymbol: null } field
                when Is(field.Type, declaring) => field.Name,
            IPropertySymbol { IsStatic: true, GetMethod: not null } property when Is(property.Type, declaring) =>
                property.Name,
            _ => null,
        };

    /// <summary>
    /// Whether a static member's type makes it one of the set — its own type, or a base of it, which is how a
    /// type-safe enum with per-member subclasses declares its members.
    /// </summary>
    private static bool Is(ITypeSymbol type, INamedTypeSymbol declaring)
    {
        for (var candidate = type; candidate is not null; candidate = candidate.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate, declaring))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Every named type this compilation declares, nested ones included. Walked rather than reached through
    /// a symbol action, because both rules are compilation-wide claims about a set and its words together.
    /// </summary>
    public static IEnumerable<INamedTypeSymbol> DeclaredTypes(Compilation compilation)
    {
        var pending = new Stack<INamespaceOrTypeSymbol>();
        pending.Push(compilation.Assembly.GlobalNamespace);

        while (pending.Count > 0)
        {
            foreach (var member in pending.Pop().GetMembers())
            {
                if (member is INamespaceSymbol @namespace)
                {
                    pending.Push(@namespace);
                }
                else if (member is INamedTypeSymbol type)
                {
                    pending.Push(type);
                    yield return type;
                }
            }
        }
    }
}
