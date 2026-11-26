---
title: <our-dictionary>
description: Outputs a dictionary translation for the current page's language, with fallbacks.
addedIn: 0.1.0
usage: element
---

`<our-dictionary>` outputs a translation from Umbraco's Translation section in the language of the current page.

```cshtml
<h3><our-dictionary key="home">My Header</our-dictionary></h3>
<h3><our-dictionary key="home" fallback-lang="da-DK">My Header</our-dictionary></h3>
```

## How a translation is chosen

1. It finds the dictionary item with the `key`.
2. It uses the translation for the current page's culture.
3. If there isn't one and `fallback-lang` is set, it uses the translation for that language.
4. If there's still no translation, the content inside the tag is left as it is.

## Attributes

| Attribute | What it does |
|---|---|
| `key` | The dictionary item's key. |
| `fallback-lang` | A culture, such as `da-DK`, to use when the current page's language has no translation. |

:::caution[Translations are output as HTML]
Markup in a translation is rendered, so `Hello <strong>there</strong>` shows bold text. `@Umbraco.GetDictionaryValue("home")` in Razor HTML-encodes the value instead, and shows the tags as text.

Treat dictionary items like rich text: only let trusted users edit them, and use `@Umbraco.GetDictionaryValue(...)` when a translation should always be plain text.
:::
