using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SimpleLocalizations.Generator;

/// <summary>
/// Keeps every member of a <c>[VocabularyFamily]</c> set's key authored. Where a read edge composes a key
/// from a member's own name — <c>category.{member}</c> for the heading a group renders under — no producer
/// ever names that key, so nothing notices one missing: the lookup falls back to echoing the word it was
/// handed, and the member renders as its own slug in every culture.
/// <para>
/// Runs only where the set is declared, which is where the words for it belong. Everywhere else there is
/// nothing to check and the same report would arrive once per referencing project.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VocabularyHeadings : DiagnosticAnalyzer
{
    private const string _marker = "SimpleLocalizations.VocabularyFamilyAttribute";

    private static readonly DiagnosticDescriptor _rule = new(
        "SL1011",
        "A declared family member has no key authored",
        "'{0}' authors no '{1}' entry in this project's vocabularies; add it, or the member renders as its own name",
        "SimpleLocalizations",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    private static readonly DiagnosticDescriptor _empty = new(
        "SL1014",
        "A declared family enumerates no members",
        "'{0}' is marked [VocabularyFamily] but enumerates no members; a set is its enum members, its constants, or its static readonly instances",
        "SimpleLocalizations",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    private static readonly ImmutableArray<DiagnosticDescriptor> _supported =
        ImmutableArray.Create(_rule, _empty);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supported;

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationAction(Inspect);
    }

    private static void Inspect(CompilationAnalysisContext context)
    {
        var sets = Declared(context.Compilation);
        if (sets.Count == 0)
        {
            return;
        }

        var authored = VocabularyKeys.Authored(context.Options, context.CancellationToken);

        foreach (var (set, prefix) in sets)
        {
            var members = VocabularyKeys.Members(set).ToList();

            // A set nothing can enumerate checks nothing and would say nothing, which reads exactly like a
            // set whose every member is authored.
            if (members.Count == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    _empty, set.Locations.FirstOrDefault(), set.Name));
                continue;
            }

            foreach (var (name, word, locations) in members)
            {
                var key = prefix + "." + word;

                if (!authored.Contains(key))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        _rule, locations.FirstOrDefault(), name, key));
                }
            }
        }
    }

    /// <summary>
    /// The sets this compilation declares, with the family each authors under. Declared here rather than
    /// referenced from elsewhere: a set's members and the words for them move together or not at all.
    /// </summary>
    private static List<(INamedTypeSymbol Set, string Prefix)> Declared(Compilation compilation)
    {
        var found = new List<(INamedTypeSymbol, string)>();

        foreach (var type in VocabularyKeys.DeclaredTypes(compilation))
        {
            foreach (var attribute in type.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == _marker
                    && attribute.ConstructorArguments.Length == 1
                    && attribute.ConstructorArguments[0].Value is string prefix
                    && prefix.Length > 0)
                {
                    found.Add((type, prefix));
                }
            }
        }

        return found;
    }
}
