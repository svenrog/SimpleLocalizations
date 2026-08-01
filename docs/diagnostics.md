# Diagnostics

[← README](../README.md)

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
