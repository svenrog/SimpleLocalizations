# Review fixes

Working document for the `chore/review-fixes` branch. One commit per item; each item is ticked here in the
same commit that fixes it. **Delete this file before merging.**

Numbering follows the review that produced it. "Verified" means the defect was reproduced against the real
harness before it was written down.

## Correctness

- [x] **1. Two resources sharing a filename crash the generator** — `VocabularyGenerator.HintName` uses the
      file name alone, so `Localization/Strings.resx` and `Shared/Strings.resx` both emit `Strings.g.cs`.
      Verified: `CS8785`, and the generator contributes *nothing* — every vocabulary in the project vanishes.
      Fix: include the resource's directory in the hint name.

- [x] **2. `[VocabularyKey]` on a nested type emits a different type** *(adds `SL1015`)* — `KeyTypeGenerator.Describe` reads
      `ContainingNamespace` and ignores `ContainingType`. Verified: a struct nested in `Outer` gets a
      top-level `Probe.NestedKey` with a full body, while the nested type keeps none. Fix: re-emit the
      enclosing `partial` chain.

- [x] **3. `Neutral` returns an unfilled template when given no args** *(`LocalizedText.From` had it too)* — `Template.Fill` short-circuits on
      an empty arg list and hands the template back verbatim. Verified: `Neutral("composed")` yields
      `found {0} of {1}`, on the path whose whole purpose is text that gets stored. Fix: fill unconditionally.

- [x] **4. `SL1011` fires on any `const string` on a family set** — the member test matches every constant
      regardless of accessibility. Verified: a `private const string` demands a key. Fix: only public members
      are members, which is what a closed set exposes.

- [x] **5. `Negotiate` never matches a language-only tag** — matching is exact-name only. Verified: with
      `en-US` and `sv-SE` supported, `sv`, `en` and `sv-FI` all fall to the neutral culture. Fix: fall back to
      a parent-culture match after the exact one.

- [ ] **6. `Truncated` emits a leading separator for a non-positive `max`** — verified:
      `Truncated(["a","b"], 0)` yields `, +2 more`.

## Design

- [ ] **7. One malformed key suppresses the whole vocabulary** — verified: a single bad key emits `SL1001`
      and zero sources, so every *valid* key's call sites fail with `CS0117` naming no resource file. That is
      the cascade `SL1004` and `SL1005` exist to avoid. Fix: skip the entry, emit the rest.

- [ ] **8. Every diagnostic is `Location.None`** — a resx diagnostic names its key but no position, so it
      cannot be navigated to or suppressed. Fix: locate into the `AdditionalText`.

- [ ] **9. Drop `ILocalizedTextSource`** — no implementations, consumers or tests. Decision: remove it.

- [ ] **10. `ListFormatter` cannot reach a consumer's patterns** — the resource set is this assembly's, so
      `scope.md`'s "ship a satellite beside it" is not reachable for a consumer. Decision: accept a
      caller-supplied catalog.

## Claims that are not true

- [ ] **11. The harness stubs the metadata it says it does not** — the csproj and `CLAUDE.md` both justify
      the hand-rolled harness by "the metadata goes through the same parser that drops a `;`". It goes
      through a `Dictionary`. Nothing exercises `SL1009` or the truncation. Fix: parse a real `.editorconfig`.

- [ ] **12. README relative links break in the packed readme** — `LICENSE.txt`,
      `./THIRD-PARTY-NOTICES.md` and `./CHANGELOG.md` 404 on nuget.org.

## Build

- [ ] **13. `ContinuousIntegrationBuild` is set on a `--no-build` pack** — it is a compile-time property, so
      it reaches nothing. Fix: set it on the build step.

## Performance

- [ ] **14. The family rule recomputes its member set per entry** — `VocabularyKeyFamilies` enumerates the
      set's symbols once per authored key. Fix: hoist per key type.

## Minor

- [ ] **15a.** `default(LocalizedText).Text` and `default(LocalizationKey).Key` are null under non-nullable
      annotations.
- [ ] **15b.** `VocabularyKeyTypes.Parse` silently overwrites a duplicated family entry.
- [ ] **15c.** `README.md` uses hard tabs for comment alignment in one example.
- [ ] **15d.** `.editorconfig` has no `[*]` section.
