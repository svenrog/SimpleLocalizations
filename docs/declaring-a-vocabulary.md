# Declaring a vocabulary

[← README](../README.md) · Next: [Diagnostics](diagnostics.md)

Everything except the keys themselves is metadata on the `VocabularyResource` item — and **every setting
defaults**, so a bare `Include` is a whole declaration.

| Metadata | Default | What it settles |
| --- | --- | --- |
| `VocabularyClass` | `{Filename}Keys` | The generated static class's name. |
| `VocabularyNamespace` | `$(RootNamespace)` + the folder | Where it lands. |
| `VocabularyResourceName` | the same, plus the file name | The base name `Catalog()` resolves against. |
| `VocabularyKeyType` | `SimpleLocalizations.LocalizationKey` | One default type, optionally followed by `family=Type` entries. |
| `VocabularyDerived` | none | Suffixes a read edge appends to another key. No member is generated for them. |

Two things worth knowing about the values:

- **Key types are fully qualified**, with no `using` in sight, because they are written into generated source
  as `global::{type}`.
- **A derived suffix matches a whole trailing segment.** `VocabularyDerived="detail|pitch"` drops
  `finding.tls.detail` and `finding.tls.pitch`, leaving `finding.tls` as the only member generated.

Anything a *type* claims is declared in code instead, on that type — see [typed keys](typed-keys.md).

## Declaring is not embedding

The item is what the **generator** reads: the package's targets add it to `AdditionalFiles`, carrying its
metadata along.

Embedding is unchanged — the SDK's own `.resx` glob still does that. Declaring a vocabulary does not embed a
resource, and does not embed it twice.

## The one non-obvious constraint

> **Every list-shaped value is separated by `|`, never `;`.**
>
> The metadata reaches the generator through a generated `.editorconfig`, where `;` begins a comment. A
> declaration written with MSBuild's own list separator therefore arrives truncated to its first entry,
> generates the wrong key types, and compiles clean.
>
> `SL1009` catches it in MSBuild — the last place the whole value still exists.

## What gets generated

`cookies.insecure` becomes `SecurityKeys.Cookies.Insecure`:

<!-- illustrative: generated source, shown with its global:: qualifiers elided -->
```csharp
public static class SecurityKeys
{
    public const string ResourceName = "MyApp.Localization.SecurityStrings";

    public static SimpleLocalizations.StringCatalog Catalog(SimpleLocalizations.TextCultures cultures) =>
        cultures.Catalog(ResourceName, typeof(SecurityKeys).Assembly);

    public static class Cookies
    {
        public const string Prefix = "cookies.";

        public static bool Covers(string key) => key.StartsWith(Prefix, StringComparison.Ordinal);

        /// <summary>The resx comment, if the entry carries one.</summary>
        public static MyApp.FindingKey Insecure => MyApp.FindingKey.From("cookies.insecure");
    }
}
```

(Type names are `global::`-qualified in the real output; elided here for reading.)

Four members come for free:

- **`ResourceName` and `Catalog`** mean the resource's base name is never spelled at a call site. It is the
  one string a consumer would otherwise have to get right and could not check — a wrong one resolves nothing
  and throws on first use.
- **`Prefix` and `Covers`** make matching a family a member call, rather than a hand-written constant and a
  `StartsWith` at the call site.

A segment that would collide with one of those four takes a `Key` suffix instead: a `catalog` key becomes
`CatalogKey`. (A segment naming its *enclosing* class is `SL1008` — a refusal, not a rename.)

## Empty values are deliberate

An empty value is not a missing translation. It says *the identity is mine, the words are the data's* — a
record titled by the thing it detected, a contact titled by a person's name.

The key is still generated, because it is stored and matched. `TryGet` reads it as absent, so a read edge
falls back to whatever was stored.

## Culture files are override lists

The **unsuffixed resx is the neutral set** and holds every key. A culture file holds only what differs, and
`ResourceManager` falls back for the rest.

A key absent from *every* culture throws, deliberately. Text rendering as its own key is how a
half-translated build reaches a customer.
