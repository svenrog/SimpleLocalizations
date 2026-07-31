using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SimpleLocalizations.Generator;

/// <summary>
/// Keeps a key's family a declared member. Where a record is filed under its key's first segment and carries
/// no grouping of its own, a key authored outside the declared set names a grouping nothing can produce — and
/// the producer has nothing to correct it with.
/// <para>
/// Declared once for the whole build rather than per resource, because it is a claim about a key
/// <em>type</em> and every project authoring one is held to it:
/// <c>&lt;VocabularyKeyFamilies&gt;My.KeyType=My.Categories&lt;/VocabularyKeyFamilies&gt;</c>, pairs
/// separated by <c>|</c>. Only keys of the named type are held to it — a key of another type files nothing.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VocabularyKeyFamilies : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor _rule = new(
        "SL1012",
        "A key is authored outside the declared families",
        "'{0}' in {1} is a {2} whose family '{3}' names no member of {4}; a key of that type is filed under its family",
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

        if (!options.GlobalOptions.TryGetValue(VocabularyMetadata.KeyFamilies, out var declared)
            || string.IsNullOrWhiteSpace(declared))
        {
            return;
        }

        var byKeyType = new Dictionary<string, ImmutableHashSet<string>>(StringComparer.Ordinal);

        foreach (var pair in PairList.Read(declared))
        {
            // Silent when the set is not on this compilation, for the reason SL1011 states: a project may
            // author keys of the type without referencing the assembly the families are declared in.
            if (context.Compilation.GetTypeByMetadataName(pair.Value) is { } members)
            {
                byKeyType[pair.Key] = VocabularyMetadata.Constants(members)
                    .Select(member => member.Name.ToLowerInvariant())
                    .ToImmutableHashSet(StringComparer.Ordinal);
            }
        }

        if (byKeyType.Count == 0)
        {
            return;
        }

        foreach (var file in context.Options.AdditionalFiles)
        {
            if (!VocabularyMetadata.IsVocabulary(options, file))
            {
                continue;
            }

            var keyTypes = VocabularyKeyTypes.Parse(
                VocabularyMetadata.Read(options, file, VocabularyMetadata.KeyType));

            if (keyTypes is null)
            {
                continue;
            }

            foreach (var entry in VocabularyReader.Read(file.GetText(context.CancellationToken)?.ToString() ?? "")
                ?? [])
            {
                var keyType = keyTypes.For(entry.Key);

                if (!byKeyType.TryGetValue(keyType, out var families))
                {
                    continue;
                }

                var dot = entry.Key.IndexOf('.');
                var family = dot < 0 ? entry.Key : entry.Key.Substring(0, dot);

                if (!families.Contains(family))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        _rule,
                        Location.None,
                        entry.Key,
                        Path.GetFileName(file.Path),
                        keyType,
                        family,
                        PairList.Read(declared).First(pair => pair.Key == keyType).Value));
                }
            }
        }
    }
}
