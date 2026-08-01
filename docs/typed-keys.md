# Typed keys

[← README](../README.md)

The reason this library exists rather than `IStringLocalizer`: sometimes the text *is* a key — a verdict is
filed against a record's title, a rule matches on it, a row in a database points at it. Then a key needs a
**type**, so that a key of one kind cannot stand in for another and resolve to nothing.

You declare the name; the generator writes the body, because the shape *is* the rule — a private constructor
and one factory are what keep the vocabulary closed, and a hand-rolled type that grew a public constructor
would open it again silently:

<!-- compiles: keytype -->
```csharp
[VocabularyKey]
public readonly partial record struct FindingKey;
```

A key is a **struct** — the attribute goes nowhere else — and the generator repeats whatever the declaration
says, so a plain `partial struct` and a `partial record struct` both work. `SL1013` catches the missing
`partial`.

```xml
<VocabularyResource Include="Localization/SecurityStrings.resx"
                    VocabularyClass="SecurityKeys"
                    VocabularyKeyType="MyApp.FindingKey" />
```

<!-- compiles: security -->
```csharp
var stored = catalog.Neutral(SecurityKeys.Cookies.Insecure.Key);   // what you persist
var shown = catalog.Get(SecurityKeys.Cookies.Insecure.Key);        // what this reader sees
```

The `.Key` is the string the catalog resolves, and a declared type is where you spell it: `StringCatalog`
overloads a `string` and the shipped `LocalizationKey`, and nothing else — a catalog that took every key type
would be the very substitution a declared type exists to refuse. Keys of the default type pass whole
(`catalog.Get(StringsKeys.Greeting)`).

One resource can produce several kinds at once —
`VocabularyKeyType="SimpleLocalizations.LocalizationKey | finding=MyApp.FindingKey"` gives the `finding.*`
family one type and everything else another. A family is matched on a key's **first segment**, so the entry
names that segment and nothing deeper.

## The set a key is filed under

A claim about a **type** is an attribute on that type, so a `typeof` cannot go stale and no name is written
twice:

<!-- compiles: keytype -->
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

<!-- compiles: console -->
```csharp
var greeting = catalog.Get(ConsoleKeys.Greeting);
```

Flat and nested keys can share one resource. A key type that claims `Families` still needs one, because a
flat key is its own first segment and so names no member of the set — but that is `SL1012` saying so, not a
rule about key shape.
