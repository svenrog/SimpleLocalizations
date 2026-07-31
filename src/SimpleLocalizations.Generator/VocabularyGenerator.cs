using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Text;

namespace SimpleLocalizations.Generator;

/// <summary>
/// Turns a <c>.resx</c> into the typed keys its producers name.
/// <para>
/// The resource file declares the words, the note explaining each entry, and the identity too. A key renamed
/// there moves the generated member with it, so a producer naming one that is no longer authored stops
/// compiling, and there is no second list to keep in step. Which key type a file produces is stated on the
/// resource item, because a decline key standing in for a finding key resolves to nothing and falls back to
/// English without a word of complaint.
/// </para>
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class VocabularyGenerator : IIncrementalGenerator
{
    /// <summary>
    /// The one spelling: at least two lowercase segments of letters, digits and hyphens, separated by dots. A
    /// single-segment key has no family to nest under and is reported as malformed.
    /// </summary>
    private static readonly Regex _wellFormed = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*(?:\.[a-z0-9]+(?:-[a-z0-9]+)*)+$",
        RegexOptions.None,
        TimeSpan.FromSeconds(1));

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var vocabularies = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, token) => Describe(pair.Left, pair.Right, token))
            .Where(static described => described is not null)
            .Select(static (described, _) => described!.Value);

        context.RegisterSourceOutput(vocabularies, static (production, vocabulary) => Produce(production, vocabulary));
    }

    /// <summary>
    /// The vocabulary a resource item declares itself to be, or <see langword="null"/> when it declares none —
    /// most <c>AdditionalFiles</c> are not vocabularies.
    /// </summary>
    private static Vocabulary? Describe(
        AdditionalText file, AnalyzerConfigOptionsProvider options, CancellationToken token)
    {
        var className = VocabularyMetadata.Read(options, file, VocabularyMetadata.Class);

        if (string.IsNullOrWhiteSpace(className))
        {
            return null;
        }

        return new Vocabulary(
            Path.GetFileName(file.Path),
            file.GetText(token)?.ToString() ?? "",
            className,
            VocabularyMetadata.Read(options, file, VocabularyMetadata.KeyType),
            VocabularyMetadata.Read(options, file, VocabularyMetadata.Namespace),
            VocabularyMetadata.Read(options, file, VocabularyMetadata.Derived));
    }

    private static void Produce(SourceProductionContext production, Vocabulary vocabulary)
    {
        if (string.IsNullOrWhiteSpace(vocabulary.KeyType) || string.IsNullOrWhiteSpace(vocabulary.Namespace))
        {
            production.ReportDiagnostic(Diagnostic.Create(
                VocabularyDiagnostics.Undeclared, Location.None, vocabulary.FileName));
            return;
        }

        if (VocabularyKeyTypes.Parse(vocabulary.KeyType) is not { } keyTypes)
        {
            production.ReportDiagnostic(Diagnostic.Create(
                VocabularyDiagnostics.KeyTypeMalformed, Location.None, vocabulary.FileName, vocabulary.KeyType));
            return;
        }

        if (VocabularyReader.Read(vocabulary.Resx) is not { } entries)
        {
            production.ReportDiagnostic(Diagnostic.Create(
                VocabularyDiagnostics.Unreadable, Location.None, vocabulary.FileName));
            return;
        }

        var derived = Derived(vocabulary.Derived);
        var root = VocabularyNode.Root();
        var declared = true;

        foreach (var entry in entries)
        {
            // A derived key hangs off another one by a suffix the read edge appends — a finding's supporting
            // text, a signal's pitch. It is authored, but it is not an identity anything names.
            if (derived.Any(suffix => entry.Key.EndsWith(suffix, StringComparison.Ordinal)))
            {
                continue;
            }

            if (!_wellFormed.IsMatch(entry.Key))
            {
                production.ReportDiagnostic(Diagnostic.Create(
                    VocabularyDiagnostics.Malformed, Location.None, entry.Key, vocabulary.FileName));
                declared = false;
                continue;
            }

            if (root.Add(entry) is { } taken)
            {
                production.ReportDiagnostic(Diagnostic.Create(
                    VocabularyDiagnostics.Nested, Location.None, taken.Path, vocabulary.FileName));
                declared = false;
            }
        }

        if (!declared)
        {
            return;
        }

        if (root.Children.Count == 0)
        {
            production.ReportDiagnostic(Diagnostic.Create(
                VocabularyDiagnostics.Empty, Location.None, vocabulary.FileName));
            return;
        }

        if (VocabularyCollisions.Check(
            root, vocabulary.ClassName, vocabulary.FileName, production.ReportDiagnostic))
        {
            return;
        }

        production.AddSource(
            HintName(vocabulary.FileName),
            SourceText.From(
                VocabularyEmitter.Emit(root, vocabulary.Namespace, vocabulary.ClassName, keyTypes),
                Encoding.UTF8));
    }

    /// <summary>
    /// What the generated file is called. Named after the resource file rather than the class it declares: two
    /// items can carry one <c>VocabularyClass</c> — a glob catching a culture file is the way in — and a
    /// repeated hint name crashes the generator instead of reporting anything.
    /// </summary>
    private static string HintName(string fileName) =>
        Path.GetFileNameWithoutExtension(fileName) + ".g.cs";

    /// <summary>
    /// The suffixes the read edge appends to another key — a finding's supporting text, a signal's pitch.
    /// Authored, but not an identity anything names, so no member is generated for them.
    /// <para>
    /// Separated by <c>|</c> for the transport reason <see cref="VocabularyKeyTypes.Parse"/> states.
    /// </para>
    /// </summary>
    private static string[] Derived(string declared) =>
    [
        .. declared
            .Split(['|'], StringSplitOptions.RemoveEmptyEntries)
            .Select(suffix => "." + suffix.Trim()),
    ];

    /// <summary>
    /// A resource item and what it declares. Carried as strings so the pipeline can tell one run's inputs from
    /// the last's — an <see cref="AdditionalText"/> compares by reference and would defeat that.
    /// </summary>
    private readonly struct Vocabulary : IEquatable<Vocabulary>
    {
        public Vocabulary(
            string fileName, string resx, string className, string keyType, string @namespace, string derived)
        {
            FileName = fileName;
            Resx = resx;
            ClassName = className;
            KeyType = keyType;
            Namespace = @namespace;
            Derived = derived;
        }

        public string FileName { get; }

        public string Resx { get; }

        public string ClassName { get; }

        public string KeyType { get; }

        public string Namespace { get; }

        public string Derived { get; }

        public bool Equals(Vocabulary other) =>
            FileName == other.FileName && Resx == other.Resx && ClassName == other.ClassName
            && KeyType == other.KeyType && Namespace == other.Namespace && Derived == other.Derived;

        public override bool Equals(object? obj) => obj is Vocabulary other && Equals(other);

        public override int GetHashCode() => (FileName, Resx, ClassName, KeyType, Namespace, Derived).GetHashCode();
    }
}
