using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SimpleLocalizations.Tests;

/// <summary>
/// The fenced code in this repository's markdown, read off disk. The documentation is the source: a test
/// holding its own copy of an example proves the copy compiles and says nothing about what a reader is told.
/// <para>
/// Nothing here knows what a block means. A fence may carry a marker — an HTML comment on the line above,
/// which the rendered page does not show — and what the kinds are worth is the reading test's business.
/// </para>
/// </summary>
internal static partial class DocumentedCode
{
    /// <summary>Directories holding build output rather than documentation.</summary>
    private static readonly string[] _ignored = ["bin", "obj", "artifacts", "TestResults", ".git", ".vs"];

    /// <summary>
    /// A fence, with the marker above it if it carries one: <c>&lt;!-- kind: argument --&gt;</c>. The marker
    /// is optional so that one pass finds both the blocks a test reads and the blocks nothing does.
    /// </summary>
    [GeneratedRegex(
        @"(?:<!--[ \t]*(?<kind>[A-Za-z]+):[ \t]*(?<argument>[^>]*?)[ \t]*-->\r?\n)?"
        + @"```(?<language>[A-Za-z0-9#+-]*)\r?\n(?<code>.*?)\r?\n```",
        RegexOptions.Singleline | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 5000)]
    private static partial Regex Fence();

    /// <summary>A block, where it is, and what it says about itself.</summary>
    public readonly record struct Block(
        string Source, int Line, string Language, string Kind, string Argument, string Code)
    {
        /// <summary>Whether the fence carries a marker at all.</summary>
        public bool IsMarked => Kind.Length > 0;

        /// <summary>The block, named the way a failure has to name it to be findable.</summary>
        public override string ToString() => $"{Source}:{Line}";
    }

    /// <summary>Every fence of <paramref name="language"/>, marked or not, across every markdown file.</summary>
    public static IEnumerable<Block> Blocks(string language)
    {
        foreach (var file in Documents())
        {
            var text = File.ReadAllText(file);

            foreach (var match in Fence().Matches(text).Cast<Match>())
            {
                if (!string.Equals(match.Groups["language"].Value, language, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return new Block(
                    Relative(file),
                    text.Take(match.Index).Count(character => character == '\n') + 1,
                    language,
                    match.Groups["kind"].Value,
                    match.Groups["argument"].Value,
                    match.Groups["code"].Value);
            }
        }
    }

    /// <summary>Every markdown file in the repository, build output aside.</summary>
    private static IEnumerable<string> Documents() =>
        Directory
            .EnumerateFiles(Root(), "*.md", SearchOption.AllDirectories)
            .Where(file => !Relative(file).Split('/').Any(segment => _ignored.Contains(segment)))
            .OrderBy(file => file, StringComparer.Ordinal);

    private static string Relative(string file) =>
        Path.GetRelativePath(Root(), file).Replace('\\', '/');

    /// <summary>
    /// The repository root, from this file's own compile-time path: the documentation is not copied to the
    /// output directory, and a test that read a copy would be back to proving nothing.
    /// </summary>
    private static string Root([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));
}
