# Traditional Chinese development copy — phase one

Base: DuelLinksAccess v1.4.2, commit 7259d9a1b8bc74ce7e657df790776d81d262eee1.

This copy embeds UTF-8 catalogs in the DLL. The original 443 English entries are
preserved verbatim. New locales can add a resource catalog and a supported locale
mapping in LocalizationCatalog.Load. Existing Loc.Get calls retain their keys.

The MelonLoader preference category `DuelLinksAccess`, entry `Language`, selects
`zh-TW` (default for this development copy) or `en`. Unsupported values select
English. A missing translated key falls back to English. Game-language detection
is deliberately not inferred from Windows/Steam language. Preferences are only
created when a user later runs the mod; building does not modify preferences.

The completed catalog translates all 447 English resource keys, including the
246 remaining Shop, Home, map, deck, accessories, trader, catalog, transfer,
shortcut and ticket messages added on 2026-10-03. English remains the fallback
for future untranslated keys. This covers catalog resources, not every English
literal or internal object name in the program. Game-provided card names/descriptions,
dialog text and Unity/Ygom text are passed through unchanged.

ContainsJapanese delegates to a portable policy recognizing kana only, including
halfwidth/extended Katakana. Traditional/Simplified Chinese and shared Han-only
labels are retained by every existing caller in Home, Dialog and tutorial paths.
Han-only Japanese text is ambiguous and is also retained rather than guessed.

Build with deployment disabled:

```powershell
dotnet test tests/DuelLinksAccess.Tests/DuelLinksAccess.Tests.csproj -c Release
dotnet build DuelLinksAccess.csproj -c Release -p:DeployToMods=false -p:SkipCopyToMods=true
```

The game installation supplies read-only IL2CPP/MelonLoader references. No
deployment or game launch is part of this development phase. Runtime/NVDA QA is
pending separate user authorization.
