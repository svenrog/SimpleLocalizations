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
<PackageReference Include="SimpleLocalizations" Version="0.1.0" />
```

Declare a key type — one per *kind* of thing you key, because a key of one kind standing in for another
resolves to nothing and falls back without a word of complaint:

```csharp
using SimpleLocalizations;

[VocabularyKey]
public readonly record struct FindingKey
{
    private FindingKey(string key) => Key = key;
    public string Key { get; }
    public static FindingKey From(string key) => new(key);   // only the generated declaration may call this
}
```

Author the words in a `.resx`, keyed `family.name`, lowercase and dotted:

```xml
<data name="security.cookies.insecure" xml:space="preserve">
  <value>Session cookie sent without Secure</value>
  <comment>Becomes the generated member's XmlDoc.</comment>
</data>
```

Point the generator at it:

```xml
<ItemGroup>
  <VocabularyResource Include="Localization/SecurityStrings.resx"
                      VocabularyClass="SecurityKeys"
                      VocabularyNamespace="MyApp.Security"
                      VocabularyKeyType="MyApp.FindingKey" />
</ItemGroup>
```

And name the member, never the key:

```csharp
var cultures = new TextCultures("en-US", "en-GB", "sv-SE");
var catalog = cultures.Catalog("MyApp.Security.SecurityStrings", typeof(Thing).Assembly);

var stored = catalog.Neutral(SecurityKeys.Security.Cookies.Insecure.Key);   // what you persist
var shown  = catalog.Get(SecurityKeys.Security.Cookies.Insecure.Key);       // what this reader sees
```

## Declaring a vocabulary

Everything but the keys themselves is metadata on the `VocabularyResource` item. Metadata carries over from
the source item, so an `EmbeddedResource` glob and the generator read one declaration.

| Metadata | What it settles |
| --- | --- |
| `VocabularyClass` | The generated static class's name. |
| `VocabularyNamespace` | Where it lands. |
| `VocabularyKeyType` | One default type, optionally followed by `family=Type` entries. |
| `VocabularyDerived` | Suffixes a read edge appends to another key, so no member is generated for them. |
| `VocabularyHeadings` | `family=Type` pairs whose members must each have a key authored (`SL1011`). |

Plus one MSBuild property, because it is a claim about a key *type* and every project authoring one is held
to it:

| Property | What it settles |
| --- | --- |
| `VocabularyKeyFamilies` | `KeyType=MembersType` pairs: a key of that type must be filed under a member (`SL1012`). |

> **Every list-shaped value is separated by `|`, never `;`.** The metadata reaches the generator through a
> generated `.editorconfig`, where `;` begins a comment — so a declaration written with MSBuild's own list
> separator arrives truncated to its first entry, generates the wrong key types, and compiles clean.
> `SL1009` catches it in MSBuild, which is the last place the whole value still exists.

## Generated shape

`security.cookies.insecure` becomes `SecurityKeys.Security.Cookies.Insecure`. Each family also carries a
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
| `SL1001` | A key that is not two or more lowercase dotted segments of letters, digits and hyphens. |
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
