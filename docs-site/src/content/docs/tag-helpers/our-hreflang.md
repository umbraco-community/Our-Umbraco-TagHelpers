---
title: <our-hreflang>
description: Outputs the hreflang alternate links for each language version of the current page.
addedIn: 2.1.0
usage: element
---

`<our-hreflang>` outputs the `hreflang` alternate links for the current page, which search engines use to find each language version of it. Put it in the `<head>` of a multilingual site:

```cshtml
<head>
    <our-hreflang />
</head>
```

On `/en/about/`, for a page published in English, Danish and Spanish, it renders:

```html
<link rel="alternate" hreflang="en-US" href="https://example.com/en/about/" />
<link rel="alternate" hreflang="da" href="https://example.com/da/om-os/" />
<link rel="alternate" hreflang="es" href="https://example.com/es/acerca/" />
<link rel="alternate" hreflang="x-default" href="https://example.com/en/about/" />
```

- There's a link for each culture the page is **published** in, including the current one, with **absolute** URLs from Umbraco's URL provider, so culture domains on the same or different hosts work.
- `hreflang` is the culture's ISO code as configured in Umbraco.
- Nothing is output for content that doesn't vary by culture, a page published in only one culture, or a request that isn't for an Umbraco page.

## The x-default link

The `x-default` link is for visitors whose language isn't one of the site's. The `x-default` attribute takes **either a culture's ISO code or `true`/`false`**:

| `x-default` | The `x-default` link points to |
|---|---|
| not set (or `"true"`) | the default language's version |
| a culture's ISO code as set up in Umbraco, such as `"da"` or `"en-US"` | that culture's version |
| `"false"` | no `x-default` link |

```cshtml
<our-hreflang />                    @* x-default: the default language *@
<our-hreflang x-default="da" />     @* x-default: the Danish version *@
<our-hreflang x-default="false" />  @* no x-default link *@
```

Values ignore case. If the culture isn't a language of the site, or the page isn't published in it, the `x-default` link is left out.
