# Changelog

All notable changes to this project are documented here.

## Unreleased

- The package ships an icon, and the notice for it: the globe emoji from Google's Noto Emoji, Apache-2.0,
  documented in `THIRD-PARTY-NOTICES.md`.

### Breaking

- `TextKey` is now `LocalizationKey` — the name a consumer is most likely to have taken already, and so the
  one a `using SimpleLocalizations` is most likely to collide with.
- `ILocalizedTextSource` is gone. Nothing in the package produced or consumed it, and a seam with nothing on
  either side of it is a shape a consumer has to match for no benefit — the composition it was meant to
  describe is the project graph's, so it belongs where that is declared.
- `Template.Fill` takes `IReadOnlyList<object?>` rather than `IReadOnlyList<string>`, so both fill edges
  reach it. A `string[]` still satisfies the call.
- `Template.Fill` no longer returns the template unchanged for an empty argument list, so a template with
  holes and nothing to fill them throws instead of rendering `{0}` at a reader — or storing it.
- `ListFormatter.Truncated` refuses a cap below one, which rendered as a separator with no item before it.

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
