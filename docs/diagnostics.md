# Diagnostics

[← README](../README.md) · Next: [Scope](scope.md)

Fifteen rules, ids `SL1001`–`SL1015`, category `SimpleLocalizations`. All ship at **warning** severity.

## The set

**Authoring keys**

| Id | What it refuses |
| --- | --- |
| `SL1001` | A key that is not lowercase segments of letters, digits and hyphens, separated by dots. |
| `SL1002` | A key that is a proper prefix of another — the segment is the family others nest under. |
| `SL1007` | Two keys whose segments produce one member name. |
| `SL1008` | A segment naming the class it would be emitted into. |

**Declaring a vocabulary**

| Id | What it refuses |
| --- | --- |
| `SL1003` | A resource marked as a vocabulary with no `VocabularyKeyType`, or no namespace to emit into. |
| `SL1004` | A resource the generator cannot read: unparseable, or a root that is not a resx `<root>`. |
| `SL1005` | A readable resource authoring no key. |
| `SL1006` | A `VocabularyKeyType` that is not one default optionally followed by `family=Type` entries. |
| `SL1009` | A list-shaped declaration containing `;`, which never reaches the generator. |

**Key types and their sets**

| Id | What it refuses |
| --- | --- |
| `SL1010` | A `From` factory reached anywhere but its generated declaration — called, or handed on as a method group. |
| `SL1011` | A member of a declared set with no key authored for it. |
| `SL1012` | A key of the declared type whose family names no member of the declared set. |
| `SL1013` | A `[VocabularyKey]` type that is not `partial`, so its body cannot be written. |
| `SL1014` | A `[VocabularyFamily]` type that enumerates no members, so it checks nothing. |
| `SL1015` | A `[VocabularyKey]` type nested in a type the generator cannot reopen — one that is not `partial`, or generic. |

## Making them fatal

They ship as warnings so each project escalates through its own strictness — `TreatWarningsAsErrors`, or
`<WarningsAsErrors>SL1001;SL1002;…</WarningsAsErrors>` for a chosen few.

Two caveats:

- **Escalate `SL1003`–`SL1006`.** They fire when the generator emits *nothing*. Left as warnings, the build
  fails anyway — as a pile of `CS0117`/`CS0246` at call sites, naming no resource file. That is exactly the
  failure they exist to prevent.
- **`SL1009` needs `MSBuildTreatWarningsAsErrors`.** It is an MSBuild warning rather than a compiler one, so
  `TreatWarningsAsErrors` does not reach it.
