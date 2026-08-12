using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Text;

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
    /// A name is <see cref="Kebab"/>-cased and a value is taken as authored, both lowercased — a key is
    /// lowercase wherever it is authored.
    /// </para>
    /// <para>
    /// <b>Public</b> whichever it is, too: a set is what it exposes, and a member nothing outside the type
    /// can name is not one a read edge composes a key from. Without that, a private constant holding a magic
    /// string — an implementation detail on the same type — demanded words authored for it.
    /// </para>
    /// </summary>
    public static IEnumerable<(string Name, string Word, ImmutableArray<Location> Locations)> Members(
        INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (Spelling(member, type) is { } word)
            {
                yield return (member.Name, word, member.Locations);
            }
        }
    }

    /// <summary>The word <paramref name="member"/> is spelled by, or <c>null</c> when it is not a member.</summary>
    private static string? Spelling(ISymbol member, INamedTypeSymbol declaring) =>
        member switch
        {
            IFieldSymbol { HasConstantValue: true, ConstantValue: string value } when value.Length > 0 =>
                value.ToLowerInvariant(),
            IFieldSymbol { HasConstantValue: true } field => Kebab(field.Name),
            // AssociatedSymbol excludes an auto-property's backing field, which is a static readonly field of
            // the declaring type and would otherwise count its property twice.
            IFieldSymbol { IsStatic: true, IsReadOnly: true, AssociatedSymbol: null } field
                when Is(field.Type, declaring) => Kebab(field.Name),
            IPropertySymbol { IsStatic: true, GetMethod: not null } property when Is(property.Type, declaring) =>
                Kebab(property.Name),
            _ => null,
        };

    /// <summary>
    /// A member name as the key spelling of it: lowercased, with a hyphen where a word begins, so
    /// <c>NotAFit</c> is <c>not-a-fit</c>.
    /// <para>
    /// A run of capitals is <b>not</b> held together — <c>TLS</c> is <c>t-l-s</c>. The two rules disagree on
    /// exactly one shape, a single-letter word before another (<c>not-a-fit</c> against <c>not-afit</c>), and
    /// only one of them can be had: a set that spells an acronym as one word declares a constant carrying the
    /// word it wants, which is what a value is read for.
    /// </para>
    /// </summary>
    private static string Kebab(string name)
    {
        var word = new StringBuilder(name.Length + 4);

        foreach (var character in name)
        {
            // An underscore separates without contributing, so a hyphen is never doubled and never leads.
            if (character == '_' || char.IsUpper(character))
            {
                if (word.Length > 0 && word[word.Length - 1] != '-')
                {
                    word.Append('-');
                }

                if (character == '_')
                {
                    continue;
                }
            }

            word.Append(char.ToLowerInvariant(character));
        }

        return word.ToString().TrimEnd('-');
    }

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
