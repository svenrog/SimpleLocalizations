# SimpleLocalizations 🌍

[![Platform](https://img.shields.io/badge/Platform-.NET%20Standard%202.0-blue.svg?style=flat)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![NuGet](https://img.shields.io/nuget/v/SimpleLocalizations)](https://www.nuget.org/packages/SimpleLocalizations)
[![Build](https://img.shields.io/github/actions/workflow/status/svenrog/SimpleLocalizations/build.yml?branch=master)](https://github.com/svenrog/SimpleLocalizations/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/github/license/svenrog/SimpleLocalizations)](LICENSE.txt)

Localized text whose **identity is stored**.

Most localization libraries answer "what does this string say in the reader's language." This one answers a
narrower question: *what do you do when the text is also a key* — when a verdict is filed against a record's
title, a rule matches on it, or a row in a database points at it. Translate that text in place and every
stored reference is orphaned. Not a crash; a quieter kind of wrong.

- Keys are identity, words are rendered late — the neutral English is stored, the reader's culture is
  resolved at the read edge.
- A `.resx` is the one declaration of both. A source generator turns it into typed key members, so a key
  renamed there moves its callers and one no longer authored stops compiling.
- The vocabulary is closed. An analyzer refuses a key invented at a call site, which would resolve to no text
  and read as a finished record.

## Usage

`dotnet add package SimpleLocalizations`

Point it at a `.resx`. That is the whole declaration:

```xml
<ItemGroup>
  <VocabularyResource Include="Strings.resx" />
</ItemGroup>
```

Author the words, keyed lowercase:

```xml
<data name="greeting" xml:space="preserve">
  <value>Hello, {0}</value>
  <comment>Becomes the generated member's XmlDoc.</comment>
</data>
```

Name the member, never the key:

<!-- compiles: strings -->
```csharp
var text = StringsKeys.Catalog(new TextCultures("en-US", "sv-SE"));

text.Format(StringsKeys.Greeting, "world");   // "Hello, world", or "Hej, world" for a Swedish reader
text.Neutral(StringsKeys.Greeting, "world");  // "Hello, world" whoever is reading — what you persist
```

`StringsKeys` is generated from `Strings.resx`; the class name, namespace, resource name and key type all
follow the file, and every one of them is overridable. Add `Strings.sv-SE.resx` holding only the entries
Swedish spells differently and it is picked up — a culture file is an override list, not a copy.

That is the whole of the simple path.

## Documentation

- [Typed keys](https://github.com/svenrog/SimpleLocalizations/blob/master/docs/typed-keys.md) — when the text
  *is* an identity, so a key needs a type of its own and a set it is filed under.
- [Declaring a vocabulary](https://github.com/svenrog/SimpleLocalizations/blob/master/docs/declaring-a-vocabulary.md)
  — the `VocabularyResource` metadata, what the generator emits, and the one non-obvious constraint.
- [Diagnostics](https://github.com/svenrog/SimpleLocalizations/blob/master/docs/diagnostics.md) — `SL1001`–`SL1014`,
  what each refuses, and which to escalate.
- [Scope](https://github.com/svenrog/SimpleLocalizations/blob/master/docs/scope.md) — what this package
  deliberately leaves to you, and why it ships a list formatter.

## Icon

The package icon is the ["globe showing Europe-Africa" emoji](https://github.com/googlefonts/noto-emoji) from
Google's Noto Emoji, used unmodified under the Apache License 2.0. It is licensed separately from the source
code above — see [THIRD-PARTY-NOTICES.md](./THIRD-PARTY-NOTICES.md).

## Package maintainer

https://github.com/svenrog

## Change log

Changes are documented in [CHANGELOG.md](./CHANGELOG.md).

## License

MIT.
