# Changelog

All notable changes to this project are documented here.

## Unreleased

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
