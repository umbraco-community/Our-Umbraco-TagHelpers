---
title: <our-bytes>
description: Shows a number of bytes, or a media item's file size, as a readable size such as 1.1 MB.
addedIn: 2.1.0
usage: element
---

`<our-bytes>` shows a number of bytes as a readable size, such as a download's file size.

```cshtml
<our-bytes media-item="@Model.Brochure" />
<our-bytes value="1152302" />
<our-bytes value="1234567" decimals="2" />
```

These render `1.1 MB` (for a 1,152,302-byte file), `1.1 MB` and `1.18 MB`, as plain text with no wrapping element:

```cshtml
<a href="@Model.Brochure.Url()">Download the brochure (<our-bytes media-item="@Model.Brochure" />)</a>
```

## Attributes

| Attribute | Type | What it does |
|---|---|---|
| `value` | `long` | A number of bytes. When it's set, it's used instead of `media-item`. |
| `media-item` | `IPublishedContent` | A media item whose file size is shown: the `umbracoBytes` property Umbraco sets when a file is uploaded. |
| `decimals` | `int` | The most decimals to show (default `1`). Trailing zeros are left out, so exactly 1 MB is `1 MB`. |

## Good to know

- Sizes go up in steps of 1024: B, KB, MB, GB, TB, PB.
- The number is formatted for the page's culture, so a Danish page shows `1,1 MB`.
- Nothing is output when there's no value, or it's negative.
