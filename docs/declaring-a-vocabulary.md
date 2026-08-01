# Declaring a vocabulary

[← README](../README.md)

Everything but the keys themselves is metadata on the `VocabularyResource` item, and **every one of them
defaults** — a bare `Include` is a whole declaration. The item is what the *generator* reads: the package's
targets add it to `AdditionalFiles` carrying its metadata, while the SDK's own `.resx` glob goes on embedding
the file. Declaring a vocabulary does not embed a resource, and does not embed it twice.

| Metadata | Default | What it settles |
| --- | --- | --- |
| `VocabularyClass` | `{Filename}Keys` | The generated static class's name. |
| `VocabularyNamespace` | `$(RootNamespace)` + the folder | Where it lands. |
| `VocabularyResourceName` | the same, plus the file name | The base name `Catalog()` resolves against. |
| `VocabularyKeyType` | `SimpleLocalizations.LocalizationKey` | One default type, optionally followed by `family=Type` entries. |
| `VocabularyDerived` | none | Suffixes a read edge appends to another key, so no member is generated for them. |

A key type is named as the generator will emit it — **fully qualified, no `using` in sight**, since it is
written into generated source as `global::{type}`. A `VocabularyDerived` suffix matches on a whole trailing
segment: `VocabularyDerived="detail|pitch"` drops `finding.tls.detail` and `finding.tls.pitch`, leaving
`finding.tls` the only member generated.

Everything a **type** claims is declared in code instead, on that type — see
[typed keys](typed-keys.md).

> **Every list-shaped value is separated by `|`, never `;`.** The metadata reaches the generator through a
> generated `.editorconfig`, where `;` begins a comment — so a declaration written with MSBuild's own list
> separator arrives truncated to its first entry, generates the wrong key types, and compiles clean.
> `SL1009` catches it in MSBuild, which is the last place the whole value still exists.

## Generated shape

`cookies.insecure` becomes `SecurityKeys.Cookies.Insecure`:

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

The `ResourceName` and `Catalog` are why the resource's base name is never spelled at a call site — the one
string a consumer would otherwise have to get right and could not check, since a wrong one resolves nothing
and throws on first use. `Prefix` and `Covers` make matching a family a member rather than a hand-written
constant and a `StartsWith` at the call site.

(Every type name is `global::`-qualified in the emitted source; elided above for reading.)

A segment that would collide with one of those four names takes a `Key` suffix instead — a `catalog` key
becomes `CatalogKey` — because the class already declares them. A segment naming its *enclosing* class is
`SL1008`, which is a refusal rather than a rename.

An **empty value** is not a missing translation — it is the declaration that *the identity is yours and the
words are the data's* (a record titled by the thing it detected, a contact titled by a person's name). The
key is still generated, because it is stored and matched; `TryGet` reads it as absent so a read edge falls
back to what was stored.

The **unsuffixed resx is the neutral set**: it holds every key, a culture file holds only what differs, and
`ResourceManager` falls back — which makes a culture file an override list rather than a copy. A key absent
from every culture throws, deliberately: text rendering as its own key is how a half-translated build reaches
a customer.
