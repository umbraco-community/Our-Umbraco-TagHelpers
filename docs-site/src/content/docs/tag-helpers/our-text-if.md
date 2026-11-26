---
title: our-text-if
description: Fills an empty element with a value, or leaves the element out when the value is empty.
addedIn: 2.1.0
usage: attribute
---

`our-text-if` goes on an **empty element**. It fills the element with the value in the attribute, or leaves the element out of the page when the value is empty. It's a shorter way to write an [`our-if`](../our-if/) that checks a value isn't empty.

```cshtml
@* Rather than *@
<h1 our-if="@(!string.IsNullOrWhiteSpace(Model.Heading))">@Model.Heading</h1>

@* you can write *@
<h1 our-text-if="@Model.Heading" />
```

| `Model.Heading` | Renders |
|---|---|
| `Welcome` | `<h1>Welcome</h1>` |
| `<em>New</em>` | `<h1>&lt;em&gt;New&lt;/em&gt;</h1>` (encoded, as `@Model.Heading` is) |
| null, empty or whitespace | nothing at all |

## Good to know

- **Use it on an empty element.** Anything you put inside the element is never shown: it's replaced by the value, or left out with the element. For content to show when the value is empty, use [`<our-fallback>`](../our-fallback/) instead.
- The value is HTML-encoded, as `@Model.Heading` is in Razor. Values that are already HTML, such as rich text (`IHtmlEncodedString`) or `IHtmlContent`, are output as HTML.
- It works with any value: `our-text-if="@Model.Value("subtitle")"` behaves the same, and a property with no value (or one that doesn't exist) leaves the element out.
- The element can be written self-closing (`<h1 ... />`). It's rendered with an end tag to hold the value.
