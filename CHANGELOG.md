# Changelog

All notable changes to this project are documented here.

## 1.0.0

- The package ships an icon, and the notice for it: the globe emoji from Google's Noto Emoji, Apache-2.0,
  documented in `THIRD-PARTY-NOTICES.md`.
- A refusal points at the entry that caused it. Every generator diagnostic carried no location, so it named a
  key and left a reader to find it in a file that may author hundreds — and could be neither navigated to in
  an IDE nor suppressed with a `#pragma`.
- A key that cannot be emitted costs itself rather than the file. One malformed key used to suppress the
  whole vocabulary, so every *other* key lost its member too and the build arrived as a pile of `CS0117` at
  call sites naming no resource file.
- A generated file is named after its folder as well as its file, so `Localization/Strings.resx` beside
  `Shared/Strings.resx` no longer collide — a repeated hint name threw inside the generator, which cost every
  vocabulary in the project rather than the colliding pair.
- A key type nested in a holder type has its body written where it was declared, rather than at the
  namespace's own level — which compiled, left the declared type with no body, and failed every call site.
- The build half costs substantially less: the emitter writes into its buffer rather than building a string
  per line, the resource file is streamed rather than loaded as a document, and the generated text reaches
  Roslyn without being copied through a string of itself. `tests/SimpleLocalizations.Benchmarks` holds the
  figures, and the ceilings that keep them are in `tests/SimpleLocalizations.Tests/Performance`.

### Breaking

- `ILocalizedTextSource` is gone. Nothing in the package produced or consumed it, and a seam with nothing on
  either side of it is a shape a consumer has to match for no benefit — the composition it was meant to
  describe is the project graph's, so it belongs where that is declared.
- `Template.Fill` takes `IReadOnlyList<object?>` rather than `IReadOnlyList<string>`, so both fill edges
  reach it. A `string[]` still satisfies the call.
- `Template.Fill` no longer returns the template unchanged for an empty argument list, so a template with
  holes and nothing to fill them throws instead of rendering `{0}` at a reader — or storing it.
- `ListFormatter.Truncated` refuses a cap below one, which rendered as a separator with no item before it.
- `ListFormatter.And`, `Or` and `Truncated` take an optional `StringCatalog`, so a culture this package ships
  no patterns for can author its own. `ListFormatter.Patterns` names the keys such a catalog owes.
- The shipped list patterns are spelled the way this package spells every other key — flat kebab rather than
  `List_And_Two` — now that `Patterns` makes them a contract a consumer authors against.

### Fixed

- `TextCultures.Negotiate` reaches a culture that shares a preference's language. It matched supported names
  exactly, so with `en-US` and `sv-SE` shipped, `sv`, `en` and `sv-FI` all fell to the neutral culture: a
  browser sends the bare language more often than not, which made reading an `Accept-Language` header — this
  method's documented job — the thing it did worst.
- `SL1011` counts only what a family set exposes. It matched every constant on a `[VocabularyFamily]` type
  regardless of accessibility, so a private constant holding a magic string demanded words authored for it.

## 0.3.0

### Breaking

- `TextKey` is now `LocalizationKey` — the name a consumer is most likely to have taken already, and so the
  one a `using SimpleLocalizations` is most likely to collide with.

## 0.2.0

- A simple path: `<VocabularyResource Include="Strings.resx" />` is a whole declaration. The class name,
  namespace, resource name and key type all default from the file, the package ships a default key type for
  a project that keys one kind of thing, and the generated class carries a `Catalog(TextCultures)` so a
  resource's base name is never spelled at a call site.
- Families are optional: a flat resource authors keys with no dot and emits members straight onto its class.
- `[VocabularyFamily]` reads any closed set — enum members, constants (spelled by their value), and static
  readonly instances of the declaring type. `SL1014` refuses one that enumerates nothing.

## 0.1.0

- Initial release: the resx-to-typed-keys generator, the closed-vocabulary analyzer, the two family rules,
  and the runtime (`StringCatalog`, `TextCultures`, `LocalizedText`, `ListFormatter`).
