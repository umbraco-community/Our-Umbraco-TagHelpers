---
title: <our-lang-switcher>
description: Renders a language switcher for every language and domain, with a Mustache template for the markup.
addedIn: 0.1.0
usage: element
---

`<our-lang-switcher>` renders a link for each language that has a culture domain set up in Umbraco, to build a language switcher. You choose the markup by writing a [Mustache](https://mustache.github.io/) template inside the element.

:::note
Each link goes to the language's domain (its home page), not to the current page in that language. Languages are found from the domains under **Culture and Hostnames**, so a site with no culture domains renders nothing. If a language has several domains, the first one is used.
:::

## With your own template

```cshtml
<our-lang-switcher>
    <div class="lang-switcher">
        {{#Languages}}
            <div class="lang-switcher__item">
                <a href="{{Url}}" lang="{{Culture}}" hreflang="{{Culture}}" class="lang-switcher__link {{#IsCurrentLang}}selected{{/IsCurrentLang}}">{{Name}}</a>
            </div>
        {{/Languages}}
    </div>
</our-lang-switcher>
```

On a site with English and Danish, that renders:

```html
<div class="lang-switcher">
    <div class="lang-switcher__item">
        <a href="https://localhost:44331/en" lang="en-US" hreflang="en-US" class="lang-switcher__link selected">English</a>
    </div>
    <div class="lang-switcher__item">
        <a href="https://localhost:44331/dk" lang="da-DK" hreflang="da-DK" class="lang-switcher__link ">dansk</a>
    </div>
</div>
```

## Template values

Inside `{{#Languages}} … {{/Languages}}`, each language has:

| Value | What it is |
|---|---|
| `{{Url}}` | The language's domain, as set in Culture and Hostnames. |
| `{{Culture}}` | The culture code, such as `en-US`. |
| `{{Name}}` | The language's name in that language, such as `English` or `dansk`. |
| `{{#IsCurrentLang}} … {{/IsCurrentLang}}` | Renders its content only for the current page's language. |

## The default template

`<our-lang-switcher />` with nothing inside uses this template:

```mustache
<ul class='lang-switcher'>
    {{#Languages}}
        <li>
            <a href='{{Url}}' lang='{{Culture}}' hreflang='{{Culture}}' class='{{#IsCurrentLang}}selected{{/IsCurrentLang}}'>
                {{Name}}
            </a>
        </li>
    {{/Languages}}
</ul>
```
