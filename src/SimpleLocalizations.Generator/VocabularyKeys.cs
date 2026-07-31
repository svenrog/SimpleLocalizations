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
    /// The constant members of <paramref name="type"/> — the words a family's members are spelled with.
    /// Constants rather than enum members alone, so a set declared as <c>const string</c> holders reads the
    /// same way.
    /// </summary>
    public static IEnumerable<IFieldSymbol> Constants(INamedTypeSymbol type) =>
        type.GetMembers().OfType<IFieldSymbol>().Where(field => field.HasConstantValue);

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
