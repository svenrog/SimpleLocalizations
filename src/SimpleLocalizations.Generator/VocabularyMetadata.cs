using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimpleLocalizations.Generator;

/// <summary>The metadata names a vocabulary is declared in, read the same way by the generator and both rules.</summary>
internal static class VocabularyMetadata
{
    public const string Class = "build_metadata.AdditionalFiles.VocabularyClass";

    public const string KeyType = "build_metadata.AdditionalFiles.VocabularyKeyType";

    public const string Namespace = "build_metadata.AdditionalFiles.VocabularyNamespace";

    public const string Derived = "build_metadata.AdditionalFiles.VocabularyDerived";

    public const string ResourceName = "build_metadata.AdditionalFiles.VocabularyResourceName";

    /// <summary>
    /// The value of <paramref name="name"/> on <paramref name="file"/>, or empty when it carries none.
    /// </summary>
    public static string Read(AnalyzerConfigOptionsProvider options, AdditionalText file, string name) =>
        options.GetOptions(file).TryGetValue(name, out var value) ? value ?? "" : "";

    /// <summary>
    /// Whether <paramref name="file"/> declares itself a vocabulary at all. Most <c>AdditionalFiles</c> are
    /// not one, and a rule that read every one of them would report against a release-notes markdown file.
    /// </summary>
    public static bool IsVocabulary(AnalyzerConfigOptionsProvider options, AdditionalText file) =>
        !string.IsNullOrWhiteSpace(Read(options, file, Class));
}
