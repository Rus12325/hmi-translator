# HmiTranslator

A tiny .NET library that a patched HMI assembly calls at runtime to translate
its UI strings. It is the companion to the **HMIPatcher** tool: the patcher
rewrites the HMI's `Language(string)` method so it becomes
`return HmiTranslator.Translator.Translate(text);`, and this assembly does the
actual lookup.

## How it works

`Translate(string)`:

1. returns the input unchanged if it is null or empty,
2. lazily loads a tab-separated `key<TAB>value` dictionary from a text file on
   the first call, under a lock,
3. caches it in a static `Dictionary<string,string>` so subsequent lookups are
   memory reads, not file I/O,
4. skips strings that are clearly widget identifiers rather than UI text
   (`bar…`, `buttonItem…`, `label…`, `panelEx…`, `superTab…`, `itemPanel…`,
   pure digits, whitespace-only),
5. logs untranslated strings so they can be added to the dictionary later,
6. returns the original text as a fallback so a missing entry can never break
   the host application.

All state is static and guarded by a `lock`, because the host application
calls `Language()` from multiple UI threads.

## Files

| File | Role |
| --- | --- |
| `Translator.cs` | First working version |
| `TranslatorMinimal.cs` | Stripped-down variant used to isolate startup problems |
| `Translator_v2.cs` | **The version `build.bat` compiles** — adds identifier filtering |
| `Translator_v3.cs` | Adds per-string de-duplication of the "untranslated" log |
| `TranslatorFinal.cs` | Polished variant with the widened filter list |

Every file declares the same type (`HmiTranslator.Translator`), so exactly one
may be compiled at a time — they are iterations of a single class kept for
reference. Add only the one you want to `HmiTranslator.csproj`.

`HmiTranslator.dll` in the repository root is the prebuilt library, ready to
drop next to the patched HMI assembly.

## Build

With the SDK-style project (recommended):

```bash
dotnet build -c Release
```

Or with the original script, which compiles `Translator_v2.cs` using the
legacy .NET Framework compiler:

```bash
build.bat
```

## Usage

This is a library — there is no executable entry point. Load it alongside the
patched assembly; `HMIPatcher` will resolve `HmiTranslator.Translator` from it.

## Notes

- The dictionary file path and the log path are hardcoded for the author's
  setup; edit the constants at the top of `Load()` to match yours.
- Keep the dictionary file UTF-8 encoded and tab-separated. Spaces instead of
  tabs are silently ignored.

## Requirements

- .NET 8 SDK, or the .NET Framework 2.0 compiler for `build.bat`
- Windows

## License

No license file is included. Contact the author before redistributing.
