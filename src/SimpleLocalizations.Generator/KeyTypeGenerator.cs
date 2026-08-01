using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace SimpleLocalizations.Generator;

/// <summary>
/// Writes the body of every <c>[VocabularyKey]</c> type.
/// <para>
/// The shape is the rule, so it is generated rather than asked for: a private constructor and one public
/// factory are what keep a vocabulary closed, and a hand-rolled type that grew a public constructor would
/// open it again with nothing failing. What a consumer declares is the name.
/// </para>
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class KeyTypeGenerator : IIncrementalGenerator
{
    private const string _marker = "SimpleLocalizations.VocabularyKeyAttribute";

    /// <summary>The pipeline stages, by the names a test asks after.</summary>
    internal static class Stages
    {
        public const string Described = "KeyTypesDescribed";

        public const string KeyTypes = "KeyTypes";
    }

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Named stages: a generator's cost is not what one run takes but how much of it a keystroke repeats,
        // and a tracking name is the only way a test can ask which stages were reused. Nothing else reads them.
        var keyTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _marker,
                static (node, _) => node is TypeDeclarationSyntax,
                static (target, _) => Describe(target))
            .WithTrackingName(Stages.Described)
            .Where(static described => described is not null)
            .Select(static (described, _) => described!.Value)
            .WithTrackingName(Stages.KeyTypes);

        context.RegisterSourceOutput(keyTypes, static (production, keyType) => Produce(production, keyType));
    }

    private static KeyType? Describe(GeneratorAttributeSyntaxContext target)
    {
        // TypeDeclarationSyntax rather than StructDeclarationSyntax: a `record struct` is a
        // RecordDeclarationSyntax, and matching only the latter would silently skip every one of them.
        if (target.TargetSymbol is not INamedTypeSymbol symbol
            || target.TargetNode is not TypeDeclarationSyntax declaration)
        {
            return null;
        }

        // Outermost first, which is the order they have to be written back out in.
        var enclosing = new List<INamedTypeSymbol>();
        for (var containing = symbol.ContainingType; containing is not null; containing = containing.ContainingType)
        {
            enclosing.Insert(0, containing);
        }

        return new KeyType(
            symbol.Name,
            symbol.ContainingNamespace.IsGlobalNamespace ? "" : symbol.ContainingNamespace.ToDisplayString(),
            declaration.Modifiers.Any(modifier => modifier.ValueText == "partial"),
            symbol.IsRecord,
            Accessibility(symbol),
            string.Join("|", enclosing.Select(type => type.Name)),
            string.Join("|", enclosing.Select(Header)),
            enclosing.FirstOrDefault(type => !CanReopen(type))?.Name ?? "");
    }

    /// <summary>
    /// Whether the generator can write a second declaration of <paramref name="type"/> to nest a key type
    /// inside it: one that is not <see langword="partial"/> cannot be reopened at all, and a generic one
    /// would have to repeat its type parameters and their constraints.
    /// </summary>
    private static bool CanReopen(INamedTypeSymbol type) =>
        type.TypeParameters.Length == 0
        && type.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(declaration => declaration.Modifiers.Any(modifier => modifier.ValueText == "partial"));

    /// <summary>An enclosing type's declaration, spelled the way a second one of it has to repeat it.</summary>
    private static string Header(INamedTypeSymbol type) =>
        $"{Accessibility(type)}{(type.IsStatic ? " static" : "")} partial "
        + $"{(type.IsRecord ? "record " : "")}{Keyword(type)} {type.Name}";

    private static string Keyword(INamedTypeSymbol type) =>
        type.TypeKind switch
        {
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            _ => "class",
        };

    private static void Produce(SourceProductionContext production, KeyType keyType)
    {
        if (!keyType.IsPartial)
        {
            production.ReportDiagnostic(Diagnostic.Create(
                VocabularyDiagnostics.NotPartial, Location.None, keyType.Name));
            return;
        }

        if (keyType.Closed.Length > 0)
        {
            production.ReportDiagnostic(Diagnostic.Create(
                VocabularyDiagnostics.NotNestable, Location.None, keyType.Name, keyType.Closed));
            return;
        }

        var source = new StringBuilder();

        source.AppendLine("// <auto-generated/>");
        source.AppendLine("#nullable enable");
        source.AppendLine();

        if (keyType.Namespace.Length > 0)
        {
            source.AppendLine($"namespace {keyType.Namespace};");
            source.AppendLine();
        }

        var enclosing = keyType.Enclosing.Length == 0
            ? []
            : keyType.Enclosing.Split('|');

        for (var depth = 0; depth < enclosing.Length; depth++)
        {
            source.AppendLine($"{Pad(depth)}{enclosing[depth]}");
            source.AppendLine($"{Pad(depth)}{{");
        }

        var pad = Pad(enclosing.Length);

        source.AppendLine($"{pad}/// <summary>");
        source.AppendLine($"{pad}/// The body of a vocabulary key: a private constructor and one factory, so a key can only come");
        source.AppendLine($"{pad}/// from the generated declaration that authors it.");
        source.AppendLine($"{pad}/// </summary>");
        source.AppendLine($"{pad}{keyType.Access} readonly partial {(keyType.IsRecord ? "record " : "")}struct {keyType.Name}");
        source.AppendLine($"{pad}{{");
        source.AppendLine($"{pad}    private readonly string? _key;");
        source.AppendLine();
        source.AppendLine($"{pad}    private {keyType.Name}(string key) => _key = key;");
        source.AppendLine();
        source.AppendLine($"{pad}    /// <summary>");
        source.AppendLine($"{pad}    /// The dotted key the catalog resolves. Computed rather than stored, because a struct is");
        source.AppendLine($"{pad}    /// always default-constructible and the signature promises no null.");
        source.AppendLine($"{pad}    /// </summary>");
        source.AppendLine($"{pad}    public string Key => _key ?? \"\";");
        source.AppendLine();
        source.AppendLine($"{pad}    /// <summary>The generated declaration's constructor.</summary>");
        source.AppendLine($"{pad}    public static {keyType.Name} From(string key) => new(key);");
        source.AppendLine();
        source.AppendLine($"{pad}    /// <inheritdoc />");
        source.AppendLine($"{pad}    public override string ToString() => Key;");
        source.AppendLine($"{pad}}}");

        for (var depth = enclosing.Length - 1; depth >= 0; depth--)
        {
            source.AppendLine($"{Pad(depth)}}}");
        }

        production.AddSource(
            $"{(keyType.Namespace.Length == 0 ? "" : keyType.Namespace + ".")}{keyType.Path}.g.cs",
            SourceText.From(source.ToString(), Encoding.UTF8));
    }

    /// <summary>
    /// The declared accessibility, spelled the way the partial has to repeat it. Anything narrower than
    /// <c>public</c> is a consumer's business — the guard is a rule, not a modifier.
    /// </summary>
    private static string Accessibility(INamedTypeSymbol symbol) =>
        symbol.DeclaredAccessibility switch
        {
            Microsoft.CodeAnalysis.Accessibility.Public => "public",
            Microsoft.CodeAnalysis.Accessibility.Internal => "internal",
            Microsoft.CodeAnalysis.Accessibility.Private => "private",
            Microsoft.CodeAnalysis.Accessibility.ProtectedAndInternal => "private protected",
            Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal => "protected internal",
            Microsoft.CodeAnalysis.Accessibility.Protected => "protected",
            _ => "internal",
        };

    /// <summary>One level of nesting, as the emitted source indents it.</summary>
    private static string Pad(int depth) => new(' ', depth * 4);

    /// <summary>
    /// A key type and the little the generator needs to write its body.
    /// <para>
    /// The enclosing types are carried as delimited strings rather than a collection: the pipeline tells one
    /// run's inputs from the last's by value, and an array compares by reference.
    /// </para>
    /// </summary>
    private readonly struct KeyType : IEquatable<KeyType>
    {
        public KeyType(
            string name, string @namespace, bool isPartial, bool isRecord, string access, string nesting,
            string enclosing, string closed)
        {
            Name = name;
            Namespace = @namespace;
            IsPartial = isPartial;
            IsRecord = isRecord;
            Access = access;
            Nesting = nesting;
            Enclosing = enclosing;
            Closed = closed;
        }

        public string Name { get; }

        public string Namespace { get; }

        public bool IsPartial { get; }

        public bool IsRecord { get; }

        public string Access { get; }

        /// <summary>The enclosing type names, outermost first, <c>|</c>-separated. Empty at the top level.</summary>
        public string Nesting { get; }

        /// <summary>The enclosing declarations to repeat, outermost first, <c>|</c>-separated.</summary>
        public string Enclosing { get; }

        /// <summary>The first enclosing type that cannot be reopened, or empty when every one can.</summary>
        public string Closed { get; }

        /// <summary>The type's dotted name below its namespace, which is what makes a hint name unique.</summary>
        public string Path =>
            Nesting.Length == 0 ? Name : Nesting.Replace('|', '.') + "." + Name;

        public bool Equals(KeyType other) =>
            Name == other.Name && Namespace == other.Namespace && IsPartial == other.IsPartial
            && IsRecord == other.IsRecord && Access == other.Access && Nesting == other.Nesting
            && Enclosing == other.Enclosing && Closed == other.Closed;

        public override bool Equals(object? obj) => obj is KeyType other && Equals(other);

        public override int GetHashCode() =>
            (Name, Namespace, IsPartial, IsRecord, Access, Nesting, Enclosing, Closed).GetHashCode();
    }
}
