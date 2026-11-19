---
title: <our-fallback>
description: Outputs a property using Umbraco's fallback modes, or the content inside the tag when there's no value.
addedIn: 0.2.0
usage: element
---

`<our-fallback>` outputs a property of the current page using Umbraco's [fallback modes](https://docs.umbraco.com/umbraco-cms/fundamentals/design/rendering-content), such as falling back to another language or to an ancestor. If there's still no value, the content inside the tag is shown instead.

```cshtml
@* The Razor way *@
@Model.Value("Header", fallback: Fallback.ToLanguage)

@* With the tag helper *@
<h3><our-fallback property="Header" mode="Fallback.ToLanguage" culture="da-DK">I do NOT have a DK culture variant of this property</our-fallback></h3>
<h3><our-fallback property="Header" mode="Fallback.ToAncestors">I do NOT have a Header property set on ANY parent and ancestors</our-fallback></h3>
```

## Attributes

| Attribute | What it does |
|---|---|
| `property` | The property on the page's model, as a ModelsBuilder property name such as `Header`. |
| `mode` | The Umbraco fallback mode, such as `Fallback.ToLanguage`, `Fallback.ToAncestors` or `Fallback.ToDefaultValue`. |
| `culture` | The culture to read the property in, such as `da-DK`. Leave it out to use the current page's culture. |

## Good to know

- The value is HTML-encoded, as `@Model.Value(...)` is in Razor. Values that are already HTML are output as HTML.
- It works with simple values such as text and numbers. A property whose model is a complex type or a collection (for example Rich Text, Markdown, pickers or blocks) renders nothing.
- The view's model must be the page (`IPublishedContent`); otherwise nothing is rendered.
