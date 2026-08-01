# SimpleLocalizations 🌍

[![Platform](https://img.shields.io/badge/Platform-.NET%20Standard%202.0-blue.svg?style=flat)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![NuGet](https://img.shields.io/nuget/v/SimpleLocalizations)](https://www.nuget.org/packages/SimpleLocalizations)
[![Build](https://img.shields.io/github/actions/workflow/status/svenrog/SimpleLocalizations/build.yml?branch=master)](https://github.com/svenrog/SimpleLocalizations/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/github/license/svenrog/SimpleLocalizations)](https://github.com/svenrog/SimpleLocalizations/blob/master/LICENSE.txt)

Localized text whose **identity is stored**.

## The problem

Most localization libraries answer one question: what does this string say in the reader's language.

This one answers a narrower one. Sometimes the text is *also a key* — a verdict is filed against a record's
title, a rule matches on it, a database row points at it. Translate that text in place and every stored
reference is orphaned. Not a crash; a quieter kind of wrong.

So this library keeps three rules, and makes the compiler hold them for you:

| Rule | What it means |
| --- | --- |
| Keys are identity | The neutral English is what you store. The reader's culture is resolved later, from the key. |
| The `.resx` is the only declaration | A generator turns it into typed members, so a renamed key moves its callers and a deleted one stops compiling. |
| The vocabulary is closed | An analyzer refuses a key invented at a call site — it would resolve to no text and read as a finished record. |

## Install

```
dotnet add package SimpleLocalizations
```

## Quick start

**1. Point at a `.resx`.** That is the whole declaration — every setting defaults from the file.

```xml
<ItemGroup>
  <VocabularyResource Include="Strings.resx" />
</ItemGroup>
```

**2. Author the words**, keyed lowercase. The `<comment>` becomes the generated member's XmlDoc.

```xml
<data name="greeting" xml:space="preserve">
  <value>Hello, {0}</value>
  <comment>Becomes the generated member's XmlDoc.</comment>
</data>
```

**3. Name the member, never the key.**

<!-- compiles: strings -->
```csharp
var text = StringsKeys.Catalog(new TextCultures("en-US", "sv-SE"));

text.Get(StringsKeys.Greeting);                // the template as authored, holes and all
text.Format(StringsKeys.Greeting, "world");    // "Hello, world", or "Hej, world" for a Swedish reader
text.Neutral(StringsKeys.Greeting, "world");   // "Hello, world" whoever is reading — what you persist
```

`StringsKeys` is generated from `Strings.resx`. The class name, namespace, resource name and key type all
follow the file, and each one is overridable.

**Adding a language?** Drop in `Strings.sv-SE.resx` holding only the entries Swedish spells differently. A
culture file is an override list, not a copy.

That is the whole of the simple path.

## Documentation

| Guide | Read it for |
| --- | --- |
| [Typed keys](https://github.com/svenrog/SimpleLocalizations/blob/master/docs/typed-keys.md) | Text that *is* an identity: giving a key its own type, and the set it is filed under. |
| [Declaring a vocabulary](https://github.com/svenrog/SimpleLocalizations/blob/master/docs/declaring-a-vocabulary.md) | Every `VocabularyResource` setting, what gets generated, and the one non-obvious constraint. |
| [Diagnostics](https://github.com/svenrog/SimpleLocalizations/blob/master/docs/diagnostics.md) | `SL1001`–`SL1015`: what each refuses, and which to make fatal. |
| [Scope](https://github.com/svenrog/SimpleLocalizations/blob/master/docs/scope.md) | What this package leaves to you, and why it ships a list formatter. |

## Icon

The package icon is the ["globe showing Europe-Africa" emoji](https://github.com/googlefonts/noto-emoji) from
Google's Noto Emoji, used unmodified under the Apache License 2.0. It is licensed separately from the source
code above — see [THIRD-PARTY-NOTICES.md](https://github.com/svenrog/SimpleLocalizations/blob/master/THIRD-PARTY-NOTICES.md).

## Package maintainer

https://github.com/svenrog

## Change log

Changes are documented in [CHANGELOG.md](https://github.com/svenrog/SimpleLocalizations/blob/master/CHANGELOG.md).

## License

MIT.
