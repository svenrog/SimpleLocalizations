using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SimpleLocalizations.Generator;

/// <summary>
/// Keeps a key's family a declared member. Where a record is filed under its key's first segment and carries
/// no grouping of its own, a key authored outside the declared set names a grouping nothing can produce — and
/// the producer has nothing to correct it with.
/// <para>
/// The claim is <c>[VocabularyKey(Families = typeof(…))]</c> on the key type, so it is stated where the type
/// is and cannot go stale: only keys of that type are held to it, and a key of another type files nothing.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VocabularyKeyFamilies : DiagnosticAnalyzer
{
    private const string _marker = "SimpleLocalizations.VocabularyKeyAttribute";

    private const string _families = "Families";

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

            var text = file.GetText(context.CancellationToken);

            foreach (var entry in VocabularyReader.Read(text?.ToString() ?? "") ?? [])
            {
                var keyType = keyTypes.For(entry.Key);

                // Silent when the type is not on this compilation, or claims no families: a project may
                // author keys of a type it cannot see the declaration of.
                if (Families(context.Compilation, keyType) is not { } families)
                {
                    continue;
                }

                var dot = entry.Key.IndexOf('.');
                var family = dot < 0 ? entry.Key : entry.Key.Substring(0, dot);

                if (!VocabularyKeys.Members(families).Any(member => member.Word == family))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        _rule,
                        text is null
                            ? VocabularyLocations.Of(file.Path)
                            : VocabularyLocations.Of(file.Path, text, entry.Span),
                        entry.Key,
                        Path.GetFileName(file.Path),
                        keyType,
                        family,
                        families.ToDisplayString()));
                }
            }
        }
    }

    /// <summary>The set <paramref name="keyType"/> claims its keys are filed under, if it claims one.</summary>
    private static INamedTypeSymbol? Families(Compilation compilation, string keyType)
    {
        if (compilation.GetTypeByMetadataName(keyType) is not { } symbol)
        {
            return null;
        }

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != _marker)
            {
                continue;
            }

            foreach (var named in attribute.NamedArguments)
            {
                if (named.Key == _families && named.Value.Value is INamedTypeSymbol families)
                {
                    return families;
                }
            }
        }

        return null;
    }
}
