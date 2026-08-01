# Typed keys

[← README](../README.md) · Next: [Declaring a vocabulary](declaring-a-vocabulary.md)

Sometimes the text *is* a key. A verdict is filed against a record's title, a rule matches on it, a database
row points at it.

Then a key needs a **type**, so a key of one kind cannot stand in for another and quietly resolve to nothing.
That is the reason this library exists rather than `IStringLocalizer`.

## Declaring a key type

You declare the name. The generator writes the body.

<!-- compiles: keytype -->
```csharp
[VocabularyKey]
public readonly partial record struct FindingKey;
```

Why generated? Because the shape *is* the rule: a private constructor and one factory are what keep the
vocabulary closed. A hand-rolled type that grew a public constructor would open it again, silently.

A key is always a **struct** — the attribute goes nowhere else — and the generator repeats whatever your
declaration says, so `partial struct` and `partial record struct` both work. Forget the `partial` and
`SL1013` says so.

## Using it

Name the type on the resource:

```xml
<VocabularyResource Include="Localization/SecurityStrings.resx"
                    VocabularyClass="SecurityKeys"
                    VocabularyKeyType="MyApp.FindingKey" />
```

Then the members carry that type:

<!-- compiles: security -->
```csharp
var stored = catalog.Neutral(SecurityKeys.Cookies.Insecure.Key);   // what you persist
var shown = catalog.Get(SecurityKeys.Cookies.Insecure.Key);        // what this reader sees
```

> **Why `.Key`?** `StringCatalog` overloads a `string` and the shipped `LocalizationKey`, and nothing else. A
> catalog that accepted every declared key type would be the very substitution a declared type exists to
> refuse — so a declared type is spelled by its `.Key`, and default-typed keys pass whole
> (`catalog.Get(StringsKeys.Greeting)`).

### More than one kind per resource

A resource can produce several kinds at once. Name the default first, then the families that differ:

```
VocabularyKeyType="SimpleLocalizations.LocalizationKey | finding=MyApp.FindingKey"
```

That gives `finding.*` one type and everything else another. A family is matched on a key's **first
segment**, so name that segment and nothing deeper.

## The set a key is filed under

A claim about a *type* belongs on that type. A `typeof` cannot go stale, and no name is written twice:

<!-- compiles: keytype -->
```csharp
[VocabularyFamily("category")]                 // every member needs a `category.{member}` key — SL1011
public enum FindingCategories { Tls, Stack, Company }

[VocabularyKey(Families = typeof(FindingCategories))]   // a key of this type is filed under one — SL1012
public readonly partial record struct FindingKey;
```

`SL1011` catches a member with no words authored for it — the case nothing else notices, since the lookup
just echoes the member's own name back in every culture.

### What counts as a member

A closed set gets written three ways, so `[VocabularyFamily]` reads all three:

| Shape | Spelled by |
| --- | --- |
| An enum member | its name |
| A constant | its **value** — a value is why it is a constant and not an enum, so `Https = "http-s"` needs `http-s` |
| A `static readonly` field or property of the declaring type | its name — the type-safe enum a class reaches for when a member needs behaviour |

Lowercased whichever it is. Anything else — a helper property, an unrelated constant — is not a member.

`SL1014` refuses a `[VocabularyFamily]` type that enumerates none of these. A set that checks nothing looks
exactly like a set whose every member is authored.

## Families are optional

A dot buys a family; nothing owes one. A **flat** resource — console furniture, a set of refusals, anything
nothing groups — authors keys with no dot:

```xml
<data name="greeting" xml:space="preserve"><value>Hello</value></data>
```

Its members land straight on the class:

<!-- compiles: console -->
```csharp
var greeting = catalog.Get(ConsoleKeys.Greeting);
```

Flat and nested keys can share one resource.

One catch: a key type that claims `Families` still needs a family, because a flat key is its own first
segment and so names no member of the set. That is `SL1012` talking, not a rule about key shape.
