# CLAUDE.md

Guidance for Claude Code in this repository.

## What this is

A localization pipeline for text whose **identity is stored**. `README.md` holds the user-facing what and
why; this file holds the where and the rules. Extracted from the Overlode repo, which is its first consumer.

Two projects, one package:

- `src/SimpleLocalizations` — the runtime, and the package. `netstandard2.0`, so it reaches .NET Framework.
- `src/SimpleLocalizations.Generator` — the Roslyn component. `netstandard2.0` because it has no choice;
  `IncludeBuildOutput` off, packed under `analyzers/dotnet/cs/` by the runtime project's `PackGeneratorAssembly`.

`build/SimpleLocalizations.targets` ships inside the package, so a `PackageReference` is the whole
installation. Its two item groups are the split that matters: the generator's inputs are conditioned on
`'@(VocabularyResource)' != ''`, while the analyzers ride NuGet's own `analyzers/` convention and are
therefore unconditional — a project authoring no resource is exactly where an invented key would otherwise
compile.

## The one non-obvious constraint

**Every list-shaped declaration is separated by `|`, never `;`.** Metadata reaches the generator through a
generated `.editorconfig`, whose parser reads `;` as the start of a comment and drops the rest of the line —
so a `;` arrives truncated to its first entry and generates the wrong key types with a clean build. This is
verified, not assumed.

The consequence for where a rule can live: `SL1009` **cannot** be a Roslyn diagnostic, because by the time
the generator runs the evidence is gone. It is an MSBuild warning in the targets file, which is the last
place the whole value still exists. Anything else that needs to see a declaration as written belongs there
too.

## Diagnostics

Ids `SL1001`–`SL1012`, category `SimpleLocalizations`, all at **warning** severity — a consumer escalates
through its own strictness. The set and what each refuses is the table in `README.md`, which is the one
statement of it; `AnalyzerReleases.Unshipped.md` is the tracked form `RS2008` requires.

Ids are a public commitment. A new rule takes the next free id and never reuses a retired one.

## Tests

`tests/SimpleLocalizations.Tests`, xUnit v3, `dotnet test`.

`VocabularyHarness` drives a real `CSharpCompilation` — resources as `AdditionalFiles`, the declaration as
analyzer-config metadata — rather than using `Microsoft.CodeAnalysis.Testing`. Deliberate: the inputs here
are exactly those two things, driving the compiler directly is less machinery, and the metadata goes through
the same parser that drops a `;`. A harness that stubbed the metadata would test a path no build takes.

Every diagnostic has a case that fires it and, where the rule has an off switch or a scope, a case proving it
stays silent. A rule with only a positive case passes when it fires on everything.

## Code style

Mirrors Overlode and SimpleCrawler (`.editorconfig`, `TreatWarningsAsErrors`):

- Primary constructors disabled (IDE0290) in `src`; test fixtures may use them.
- Private fields `_camelCase`, including static/const. One top-level class per `.cs` file, filename matches.
- Declaration comments go in `/// <summary>` XmlDoc, not `//`. Plain `//` is rare in-body rationale only.
- **Comment only what the code cannot show, only where a reader would otherwise get it wrong.** State the
  constraint, not the reasoning that found it.
- **A fact that can drift (version, count, measured size) belongs in a test, never prose.**
- `GenerateDocumentationFile` is on, so a `<see cref="…"/>` naming a renamed or deleted type fails the build.

## Git

Feature branches, PR against `master`. Tags `v*` trigger the release workflow (MinVer reads the tag).
