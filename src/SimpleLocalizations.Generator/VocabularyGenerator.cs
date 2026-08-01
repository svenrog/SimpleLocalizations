using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Text.RegularExpressions;

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
    /// The one spelling: lowercase segments of letters, digits and hyphens, separated by dots.
    /// <para>
    /// A family is what a dot buys, not something every vocabulary owes: a flat resource authors keys with no
    /// dot and emits members straight onto its class. A key type that claims families
    /// (<c>[VocabularyKey(Families = …)]</c>) still requires one, because a flat key names no member of the
    /// set — which is <c>SL1012</c>'s to say, not this rule's.
    /// </para>
    /// </summary>
    private static readonly Regex _wellFormed = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*(?:\.[a-z0-9]+(?:-[a-z0-9]+)*)*$",
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

        var hint = VocabularyMetadata.Read(options, file, VocabularyMetadata.Hint);

        return new Vocabulary(
            file.Path,
            Path.GetFileName(file.Path),
            file.GetText(token)?.ToString() ?? "",
            className,
            VocabularyMetadata.Read(options, file, VocabularyMetadata.KeyType),
            VocabularyMetadata.Read(options, file, VocabularyMetadata.Namespace),
            VocabularyMetadata.Read(options, file, VocabularyMetadata.Derived),
            VocabularyMetadata.Read(options, file, VocabularyMetadata.ResourceName),
            hint.Length > 0 ? hint : Flatten(file.Path));
    }

    private static void Produce(SourceProductionContext production, Vocabulary vocabulary)
    {
        var file = VocabularyLocations.Of(vocabulary.Path);

        if (string.IsNullOrWhiteSpace(vocabulary.KeyType) || string.IsNullOrWhiteSpace(vocabulary.Namespace))
        {
            production.ReportDiagnostic(Diagnostic.Create(
                VocabularyDiagnostics.Undeclared, file, vocabulary.FileName));
            return;
        }

        if (VocabularyKeyTypes.Parse(vocabulary.KeyType) is not { } keyTypes)
        {
            production.ReportDiagnostic(Diagnostic.Create(
                VocabularyDiagnostics.KeyTypeMalformed, file, vocabulary.FileName, vocabulary.KeyType));
            return;
        }

        if (VocabularyReader.Read(vocabulary.Resx) is not { } entries)
        {
            production.ReportDiagnostic(Diagnostic.Create(
                VocabularyDiagnostics.Unreadable, file, vocabulary.FileName));
            return;
        }

        var text = SourceText.From(vocabulary.Resx);
        Location At(VocabularyEntry? entry) =>
            entry is null ? file : VocabularyLocations.Of(vocabulary.Path, text, entry.Span);

        var derived = Derived(vocabulary.Derived);
        var root = VocabularyNode.Root();
        var refused = false;

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
                    VocabularyDiagnostics.Malformed, At(entry), entry.Key, vocabulary.FileName));
                refused = true;
                continue;
            }

            if (root.Add(entry) is { } taken)
            {
                // The key named is the one already filed; where it is a branch it authors nothing of its own,
                // so the entry that collided with it is the nearest thing in the file to point at.
                production.ReportDiagnostic(Diagnostic.Create(
                    VocabularyDiagnostics.Nested, At(taken.Entry ?? entry), taken.Path, vocabulary.FileName));
                refused = true;
            }
        }

        // A key nothing can emit costs itself, never the file. Refusing the whole vocabulary over one bad key
        // moves the failure to every *other* key's call sites as a pile of CS0117 naming no resource file —
        // which is the cascade SL1004 and SL1005 exist to keep out of a consumer's build.
        refused |= VocabularyCollisions.Prune(
            root, vocabulary.ClassName, vocabulary.FileName, At, production.ReportDiagnostic);

        if (root.Children.Count == 0)
        {
            // Silent where every key was refused one by one: those diagnostics said why, and this one would
            // only say that they did.
            if (!refused)
            {
                production.ReportDiagnostic(Diagnostic.Create(
                    VocabularyDiagnostics.Empty, file, vocabulary.FileName));
            }

            return;
        }

        production.AddSource(
            vocabulary.Hint + ".g.cs",
            SourceText.From(
                VocabularyEmitter.Emit(
                    root, vocabulary.Namespace, vocabulary.ClassName, keyTypes, vocabulary.ResourceName),
                Encoding.UTF8));
    }

    /// <summary>
    /// A resource's path as a hint name, for a build that declared none. Named after the file rather than the
    /// class it declares, because two items can carry one <c>VocabularyClass</c> — a glob catching a culture
    /// file is the way in — and after the <em>whole</em> path rather than the file name, because two folders
    /// can carry one file name. A repeated hint name throws inside the generator, which costs every
    /// vocabulary in the project rather than the colliding pair.
    /// </summary>
    private static string Flatten(string path) =>
        string.Concat(
            Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path))
                .Select(character => character is '\\' or '/' or ':' ? '.' : character))
            .Trim('.');

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
            string path, string fileName, string resx, string className, string keyType, string @namespace,
            string derived, string resourceName, string hint)
        {
            Path = path;
            FileName = fileName;
            Resx = resx;
            ClassName = className;
            KeyType = keyType;
            Namespace = @namespace;
            Derived = derived;
            ResourceName = resourceName;
            Hint = hint;
        }

        /// <summary>Where the resource is, which is where a diagnostic about it points.</summary>
        public string Path { get; }

        /// <summary>What the resource is called, which is how a message names it.</summary>
        public string FileName { get; }

        public string Resx { get; }

        public string ClassName { get; }

        public string KeyType { get; }

        public string Namespace { get; }

        public string Derived { get; }

        public string ResourceName { get; }

        /// <summary>What the generated file is called, without its extension.</summary>
        public string Hint { get; }

        public bool Equals(Vocabulary other) =>
            Path == other.Path && FileName == other.FileName && Resx == other.Resx && ClassName == other.ClassName
            && KeyType == other.KeyType && Namespace == other.Namespace && Derived == other.Derived
            && ResourceName == other.ResourceName && Hint == other.Hint;

        public override bool Equals(object? obj) => obj is Vocabulary other && Equals(other);

        public override int GetHashCode() =>
            (Path, FileName, Resx, ClassName, KeyType, Namespace, Derived, ResourceName, Hint).GetHashCode();
    }
}
