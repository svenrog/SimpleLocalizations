# Declaring a vocabulary

[← README](../README.md)

Everything but the keys themselves is metadata on the `VocabularyResource` item, and **every one of them
defaults** — a bare `Include` is a whole declaration. Metadata carries over from the source item, so an
`EmbeddedResource` glob and the generator read one declaration.

| Metadata | Default | What it settles |
| --- | --- | --- |
| `VocabularyClass` | `{FileName}Keys` | The generated static class's name. |
| `VocabularyNamespace` | `$(RootNamespace)` + the folder | Where it lands. |
| `VocabularyResourceName` | the same, plus the file name | What `Catalog()` resolves against; empty emits no factory. |
| `VocabularyKeyType` | `SimpleLocalizations.LocalizationKey` | One default type, optionally followed by `family=Type` entries. |
| `VocabularyDerived` | none | Suffixes a read edge appends to another key, so no member is generated for them. |

Everything a **type** claims is declared in code instead, on that type — see
[typed keys](typed-keys.md).

> **Every list-shaped value is separated by `|`, never `;`.** The metadata reaches the generator through a
> generated `.editorconfig`, where `;` begins a comment — so a declaration written with MSBuild's own list
> separator arrives truncated to its first entry, generates the wrong key types, and compiles clean.
> `SL1009` catches it in MSBuild, which is the last place the whole value still exists.

## Generated shape

`cookies.insecure` becomes `SecurityKeys.Cookies.Insecure`. The class carries a `ResourceName` and a
`Catalog(TextCultures)`, so the resource's base name is never spelled at a call site — the one string a
consumer would otherwise have to get right and could not check, since a wrong one resolves nothing and throws
on first use. Each family also carries a `Prefix` and a `Covers(key)`, so matching a family is a member
rather than a hand-written constant and a `StartsWith` at the call site. A resx `<comment>` becomes the
member's XmlDoc.

An **empty value** is not a missing translation — it is the declaration that *the identity is yours and the
words are the data's* (a record titled by the thing it detected, a contact titled by a person's name). The
key is still generated, because it is stored and matched; `TryGet` reads it as absent so a read edge falls
back to what was stored.

The **unsuffixed resx is the neutral set**: it holds every key, a culture file holds only what differs, and
`ResourceManager` falls back — which makes a culture file an override list rather than a copy. A key absent
from every culture throws, deliberately: text rendering as its own key is how a half-translated build reaches
a customer.
