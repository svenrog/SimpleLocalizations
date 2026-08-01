# Scope

[← README](../README.md)

## What is not here

- **A read edge.** Resolving a stored record's title through a chain of registered sources is shaped by what
  your records are; `ILocalizedTextSource` is the seam, and composing it is yours.
- **A refusal's wording.** `TextCultures.TryResolve` answers `bool` and no message. This package authors no
  resource your reader can translate, and nothing may word a refusal itself — which is also why
  `LocalizedText`'s constructor is private and its one factory takes a catalog and a key.
- **Cultures beyond `en-US`, `en-GB` and `sv-SE` for `ListFormatter`.** The list patterns are the package's
  own resource set; a consumer needing another ships a satellite beside it.

## Why a list formatter

`string.Join(", ", items)` is not a translation-neutral operation and looks like one. en-US writes the serial
(Oxford) comma before the final conjunction and en-GB does not, so the same call has to produce `a, b, and c`
for one reader and `a, b and c` for the other. .NET ships no list formatter; ICU and Java both do.

Only for lists **read as prose**. An enumeration after a colon is punctuation rather than grammar and should
stay a plain join.
