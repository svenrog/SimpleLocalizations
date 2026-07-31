# SimpleLocalizations

Localized text whose **identity is stored**.

Most localization libraries answer "what does this string say in the reader's language." This one answers a
narrower question: *what do you do when the text is also a key* — when a verdict is filed against a record's
title, a rule matches on it, or a row in a database points at it. Translate that text in place and every
stored reference is orphaned. Not a crash; a quieter kind of wrong.

The answer here is three rules, and the library exists to make the compiler hold them:

1. **Keys are identity, words are rendered late.** Nothing localizes text where a record is produced. The
   neutral English is stored, and the reader's culture is resolved from the key at the read edge.
2. **A `.resx` is the one declaration of both.** A source generator turns each resource into typed key
   members, so a key renamed there moves its callers, and one no longer authored stops compiling. There is no
   second list.
3. **The vocabulary is closed.** A key type's factory is reachable by its generated declaration and nowhere
   else, so no call site can invent an identity that resolves to no text and reads as a finished record.

## Getting started

```xml
<PackageReference Include="SimpleLocalizations" Version="0.2.0" />
```

Point it at a `.resx`. That is the whole declaration:

```xml
<ItemGroup>
  <VocabularyResource Include="Strings.resx" />
</ItemGroup>
```

Author the words, keyed lowercase — a dot nests one family under another, and nothing owes you one:

```xml
<data name="greeting" xml:space="preserve">
  <value>Hello, {0}</value>
  <comment>Becomes the generated member's XmlDoc.</comment>
</data>
```

Name the member, never the key:

```csharp
var text = StringsKeys.Catalog(new TextCultures("en-US", "sv-SE"));

text.Format(StringsKeys.Greeting, "world");   // "Hello, world", or "Hej, world" for a Swedish reader
text.Neutral(StringsKeys.Greeting, "world");  // "Hello, world" whoever is reading — what you persist
```

`StringsKeys` is generated from `Strings.resx`; the class name, namespace, resource name and key type all
follow the file, and every one of them is overridable. Add `Strings.sv-SE.resx` holding only the entries
Swedish spells differently and it is picked up — a culture file is an override list, not a copy.

That is the whole of the simple path. What follows is for the case it does not cover.

## When text is also an identity

The reason this library exists rather than `IStringLocalizer`: sometimes the text *is* a key — a verdict is
filed against a record's title, a rule matches on it, a row in a database points at it. Then a key needs a
**type**, so that a key of one kind cannot stand in for another and resolve to nothing.

You declare the name; the generator writes the body, because the shape *is* the rule — a private constructor
and one factory are what keep the vocabulary closed, and a hand-rolled type that grew a public constructor
would open it again silently:

```csharp
[VocabularyKey]
public readonly partial record struct FindingKey;
```

```xml
<VocabularyResource Include="Localization/SecurityStrings.resx"
                    VocabularyClass="SecurityKeys"
                    VocabularyKeyType="MyApp.FindingKey" />
```

```csharp
var stored = catalog.Neutral(SecurityKeys.Cookies.Insecure.Key);   // what you persist
var shown  = catalog.Get(SecurityKeys.Cookies.Insecure.Key);       // what this reader sees
```

One resource can produce several kinds at once — `VocabularyKeyType="MyApp.LocalizationKey | finding=MyApp.FindingKey"`
gives the `finding.*` family one type and everything else another.

## Declaring a vocabulary

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

Everything else is declared **in code, on the type it is about**, so a `typeof` cannot go stale and no name
is written twice:

```csharp
[VocabularyFamily("category")]                 // every member needs a `category.{member}` key — SL1011
public enum FindingCategories { Tls, Stack, Company }

[VocabularyKey(Families = typeof(FindingCategories))]   // a key of this type is filed under one — SL1012
public readonly partial record struct FindingKey;
```

### What counts as a member of a set

`[VocabularyFamily]` reads three shapes, because a closed set is written all three ways:

| Shape | Spelled by |
| --- | --- |
| An enum member | its name |
| A constant | its **value** — a value is why it is a constant and not an enum, so `Https = "http-s"` needs `http-s` |
| A `static readonly` field or property of the declaring type | its name — the type-safe enum a class reaches for when a member needs behaviour |

Lowercased whichever it is. A member whose type is something else — a helper property, an unrelated
constant — is not one of the set. `SL1014` refuses a `[VocabularyFamily]` type that enumerates none of these,
since checking nothing looks exactly like a set whose every member is authored.

### Families are optional

A dot buys a family; nothing owes one. A **flat** resource — console furniture, a set of refusals, anything
nothing groups — authors keys with no dot and emits members straight onto its class:

```xml
<data name="greeting" xml:space="preserve"><value>Hello</value></data>
```

```csharp
catalog.Get(ConsoleKeys.Greeting.Key)
```

Flat and nested keys can share one resource. A key type that claims `Families` still needs one, because a
flat key names no member of the set — but that is `SL1012` saying so, not a rule about key shape.

> **Every list-shaped value is separated by `|`, never `;`.** The metadata reaches the generator through a
> generated `.editorconfig`, where `;` begins a comment — so a declaration written with MSBuild's own list
> separator arrives truncated to its first entry, generates the wrong key types, and compiles clean.
> `SL1009` catches it in MSBuild, which is the last place the whole value still exists.

## Generated shape

`cookies.insecure` becomes `SecurityKeys.Cookies.Insecure`. The class carries a `ResourceName` and a
`Catalog(TextCultures)`, so the resource's base name is never spelled at a call site — the one string a
consumer would otherwise have to get right and could not check, since a wrong one resolves nothing and throws
on first use. Each family also carries a
`Prefix` and a `Covers(key)`, so matching a family is a member rather than a hand-written constant and a
`StartsWith` at the call site. A resx `<comment>` becomes the member's XmlDoc.

An **empty value** is not a missing translation — it is the declaration that *the identity is yours and the
words are the data's* (a record titled by the thing it detected, a contact titled by a person's name). The
key is still generated, because it is stored and matched; `TryGet` reads it as absent so a read edge falls
back to what was stored.

The **unsuffixed resx is the neutral set**: it holds every key, a culture file holds only what differs, and
`ResourceManager` falls back — which makes a culture file an override list rather than a copy. A key absent
from every culture throws, deliberately: text rendering as its own key is how a half-translated build reaches
a customer.

## Diagnostics

| Id | What it refuses |
| --- | --- |
| `SL1001` | A key that is not lowercase segments of letters, digits and hyphens, separated by dots. |
| `SL1002` | A key that is a proper prefix of another — the segment is the family others nest under. |
| `SL1003` | A resource marked as a vocabulary with no `VocabularyKeyType`. |
| `SL1004` | A resource the generator cannot read: unparseable, or a root that is not a resx `<root>`. |
| `SL1005` | A readable resource authoring no key. |
| `SL1006` | A `VocabularyKeyType` that is not one default optionally followed by `family=Type` entries. |
| `SL1007` | Two keys whose segments produce one member name. |
| `SL1008` | A segment naming the class it would be emitted into. |
| `SL1009` | A list-shaped declaration containing `;`, which never reaches the generator. |
| `SL1010` | A `From` factory reached anywhere but its generated declaration. |
| `SL1011` | A member of a declared set with no key authored for it. |
| `SL1012` | A key of the declared type whose family names no member of the declared set. |
| `SL1013` | A `[VocabularyKey]` type that is not `partial`, so its body cannot be written. |
| `SL1014` | A `[VocabularyFamily]` type that enumerates no members, so it checks nothing. |

**They ship as warnings.** Escalate them in the projects that want them fatal — `TreatWarningsAsErrors`, or
`<WarningsAsErrors>SL1001;SL1002;…</WarningsAsErrors>` for the set. Two caveats worth knowing:

- `SL1003`–`SL1006` fire when the generator emits *nothing*. Left as warnings, the build then fails with a
  pile of `CS0117`/`CS0246` at call sites naming no resource file — which is the failure they exist to
  prevent. Escalating them is strongly recommended.
- `SL1009` is an **MSBuild** warning, not a compiler one, so `TreatWarningsAsErrors` does not reach it. Set
  `MSBuildTreatWarningsAsErrors` to make it fatal.

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

## License

MIT.
