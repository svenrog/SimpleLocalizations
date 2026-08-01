# Scope

[← README](../README.md) · Back to: [Typed keys](typed-keys.md)

## What is not here

**A read edge.** Resolving a stored record's title through a chain of registered sources is shaped by what
your records are — which project owns which words, and how a spine reaches the plug-ins above it. A
`StringCatalog` per project is what this package gives you; the seam that composes them is yours to declare,
because its shape is your project graph's rather than ours.

**A refusal's wording.** `TextCultures.TryResolve` answers `bool` and no message. This package authors no
resource your reader can translate, and nothing may word a refusal on your behalf. Same reason
`LocalizedText`'s constructor is private and its one factory takes a catalog and a key.

**Cultures beyond `en-US`, `en-GB` and `sv-SE` for `ListFormatter`.** Those list patterns are the package's
own resource set, which you cannot add a satellite to without building one against this assembly. So author
the same keys in a resource of your own and pass a catalog over it:

<!-- compiles: patterns -->
```csharp
// Any type from the assembly that embeds the resource. These keys are not lowercase, so the resource is an
// ordinary EmbeddedResource rather than a VocabularyResource — nothing here is a stored identity.
var patterns = new TextCultures("de-DE").Catalog(
    "MyApp.Localization.ListPatterns", typeof(ListPatterns).Assembly);

var joined = ListFormatter.And(["a", "b", "c"], patterns);
```

`ListFormatter.Patterns` names every key such a catalog owes — read it in a parity test rather than copying
the list, so a pattern added here fails your build rather than someone's reader.

| Key | Fills with |
| --- | --- |
| `List_And_Two`, `List_Or_Two` | `{0}` and `{1}`: the only two items |
| `List_And_Middle`, `List_Or_Middle` | `{0}` the accumulated head, `{1}` the next item |
| `List_And_End`, `List_Or_End` | `{0}` the head, `{1}` the last item |
| `List_Separator` | `{0}` the head, `{1}` the next item — a plain join, no conjunction |
| `List_Truncated` | `{0}` the items named, `{1}` how many were dropped |

## Why a list formatter

`string.Join(", ", items)` is not a translation-neutral operation, and looks like one.

en-US writes the serial (Oxford) comma before the final conjunction; en-GB does not. So the same call has to
produce `a, b, and c` for one reader and `a, b and c` for the other. .NET ships no list formatter — ICU and
Java both do.

Use it only for lists **read as prose**. An enumeration after a colon is punctuation rather than grammar, and
should stay a plain join.
