using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SimpleLocalizations.Generator;

/// <summary>
/// Keeps every member of a declared set's heading authored. Where a read edge composes a key from a member's
/// own name — <c>category.{member}</c> for the heading a group renders under — no producer ever names that
/// key, so nothing notices one missing: the lookup falls back to echoing the word it was handed, and the
/// member renders as its own slug in every culture.
/// <para>
/// Declared per resource, because the headings live in exactly one file:
/// <c>VocabularyHeadings="category=My.Namespace.Categories"</c>, pairs separated by <c>|</c>.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VocabularyHeadings : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor _rule = new(
        "SL1011",
        "A declared family member has no key authored",
        "'{0}' authors no '{1}' entry in {2}; add it, or the member renders as its own name",
        "SimpleLocalizations",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    private static readonly ImmutableArray<DiagnosticDescriptor> _supported = ImmutableArray.Create(_rule);

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
        var options = context.Options.AnalyzerConfigOptionsProvider;

        foreach (var file in context.Options.AdditionalFiles)
        {
            if (!VocabularyMetadata.IsVocabulary(options, file))
            {
                continue;
            }

            var declared = VocabularyMetadata.Read(options, file, VocabularyMetadata.Headings);
            if (declared.Length == 0)
            {
                continue;
            }

            var authored = Authored(file, context.CancellationToken);

            foreach (var pair in PairList.Read(declared))
            {
                // Silent when the type is not on this compilation: the resource may be built somewhere the
                // set it heads is not referenced, and a rule cannot check what it cannot see.
                if (context.Compilation.GetTypeByMetadataName(pair.Value) is not { } members)
                {
                    continue;
                }

                foreach (var member in VocabularyMetadata.Constants(members))
                {
                    var key = pair.Key + "." + member.Name.ToLowerInvariant();

                    if (!authored.Contains(key))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            _rule,
                            member.Locations.FirstOrDefault(),
                            member.Name,
                            key,
                            Path.GetFileName(file.Path)));
                    }
                }
            }
        }
    }

    /// <summary>
    /// The keys the resource authors. Read off the resource rather than the generated members, since a family
    /// a read edge composes at run time is exactly what this rule is about.
    /// </summary>
    private static ImmutableHashSet<string> Authored(AdditionalText file, CancellationToken token) =>
        (VocabularyReader.Read(file.GetText(token)?.ToString() ?? "") ?? [])
            .Select(entry => entry.Key)
            .ToImmutableHashSet(StringComparer.Ordinal);
}
