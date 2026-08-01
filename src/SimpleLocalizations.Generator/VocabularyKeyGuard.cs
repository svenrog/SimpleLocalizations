using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SimpleLocalizations.Generator;

/// <summary>
/// Keeps the vocabulary closed: a <c>From</c> factory may be reached by its generated declaration and by
/// nothing else, whether it is called or handed on as a method group.
/// <para>
/// The reason it cannot be an accessibility modifier is in
/// <c>SimpleLocalizations.VocabularyKeyAttribute</c>, which marks the types this covers.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VocabularyKeyGuard : DiagnosticAnalyzer
{
    /// <summary>The marker a key type carries, by name — this project references no product assembly.</summary>
    private const string _marker = "SimpleLocalizations.VocabularyKeyAttribute";

    private const string _factory = "From";

    private static readonly DiagnosticDescriptor _rule = new(
        "SL1010",
        "A vocabulary key is produced outside its declaration",
        "{0}.From produces a key at a call site; name the generated member instead, and author the key in the .resx if it does not exist",
        "SimpleLocalizations",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly ImmutableArray<DiagnosticDescriptor> _supported = ImmutableArray.Create(_rule);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supported;

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();

        // The generated declaration is the one legitimate caller, so it is exactly what this must not see.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(
            Inspect, SyntaxKind.InvocationExpression, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void Inspect(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;

        // The member access inside a call is that call's to report; on its own it is a method group, which
        // hands the ability to invent a key to whatever holds the delegate.
        if (node is MemberAccessExpressionSyntax
            && node.Parent is InvocationExpressionSyntax invocation && invocation.Expression == node)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol is not IMethodSymbol
            {
                Name: _factory, IsStatic: true, ContainingType: { } declaring,
            })
        {
            return;
        }

        if (!declaring.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == _marker))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(_rule, node.GetLocation(), declaring.Name));
    }
}
