using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SimpleLocalizations.Tests;

/// <summary>
/// The C# fences in this repository's markdown, read off disk. The documentation is the source: a test
/// holding its own copy of an example proves the copy compiles and says nothing about what a reader is told.
/// </summary>
internal static class DocumentedCode
{
    /// <summary>What a fence is marked with, in an HTML comment the rendered page does not show.</summary>
    private static readonly Regex _marked = new(
        @"<!--\s*(?<kind>compiles|illustrative):\s*(?<fixture>[^\s>-][^>]*?)\s*-->\r?\n```csharp\r?\n(?<code>.*?)\r?\n```",
        RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex _fenced = new(
        @"```csharp\r?\n(?<code>.*?)\r?\n```",
        RegexOptions.Singleline | RegexOptions.CultureInvariant);

    /// <summary>A marked block: where it is, which fixture it names, and whether it claims to compile.</summary>
    public readonly record struct Block(string Source, string Fixture, string Code, bool Compiles);

    /// <summary>Every marked C# block, across every documented file.</summary>
    public static IEnumerable<Block> Blocks()
    {
        foreach (var file in Documents())
        {
            var text = File.ReadAllText(file);

            foreach (Match match in _marked.Matches(text))
            {
                yield return new Block(
                    Path.GetFileName(file),
                    match.Groups["fixture"].Value.Split(',')[0].Trim(),
                    match.Groups["code"].Value,
                    match.Groups["kind"].Value == "compiles");
            }
        }
    }

    /// <summary>The C# blocks carrying no marker, named by the file and their first line.</summary>
    public static IEnumerable<string> Unmarked()
    {
        foreach (var file in Documents())
        {
            var text = File.ReadAllText(file);
            var marked = _marked.Matches(text).Select(match => match.Groups["code"].Value).ToHashSet(StringComparer.Ordinal);

            foreach (Match match in _fenced.Matches(text))
            {
                var code = match.Groups["code"].Value;

                if (!marked.Contains(code))
                {
                    yield return $"{Path.GetFileName(file)}: {code.Split('\n')[0].Trim()}";
                }
            }
        }
    }

    /// <summary>`README.md` and everything under `docs/`.</summary>
    private static IEnumerable<string> Documents()
    {
        var root = Root();

        return
        [
            Path.Combine(root, "README.md"),
            .. Directory.EnumerateFiles(Path.Combine(root, "docs"), "*.md").OrderBy(path => path, StringComparer.Ordinal),
        ];
    }

    /// <summary>
    /// The repository root, from this file's own compile-time path: the documentation is not copied to the
    /// output directory, and a test that read a copy would be back to proving nothing.
    /// </summary>
    private static string Root([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));
}
