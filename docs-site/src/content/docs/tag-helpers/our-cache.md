---
title: <our-cache>
description: ASP.NET Core's cache tag helper, turned off in preview and debug, and cleared when content is published.
addedIn: 0.8.0
usage: element
---

`<our-cache>` is ASP.NET Core's [cache tag helper](https://learn.microsoft.com/aspnet/core/mvc/views/tag-helpers/built-in/cache-tag-helper) with two Umbraco additions:

- It's turned off in Umbraco preview and debug mode, so editors always see their latest changes.
- Its cache is cleared whenever content, media or dictionary items are published.

It takes all the same attributes as `<cache>`, such as `expires-after` and `vary-by`.

## Before and after

```cshtml title="Before"
<cache enabled="!UmbracoContext.IsDebug && !UmbracoContext.InPreviewMode">[Your long running, expensive code here]</cache>
```

```cshtml title="After"
<our-cache>[Your long running, expensive code here]</our-cache>
```

## Examples

```cshtml
<our-cache expires-on="new DateTime(2025,1,29,17,02,0)">
    <p>@DateTime.Now - until a set date and time</p>
</our-cache>

<our-cache expires-after="TimeSpan.FromSeconds(120)">
    <p>@DateTime.Now - every 120 seconds (2 minutes)</p>
</our-cache>

<our-cache>
    <!-- A global navigation that must update across the site when, say, the phone number on the home page changes -->
    <partial name="Navigation" />
</our-cache>
```

## Clearing the cache on publish

Like Umbraco's other cache helpers, such as `Html.CachedPartial`, the cache is cleared every time anything is published. That's the same as writing:

```cshtml
<our-cache update-cache-on-publish="true">[Your long running, expensive code here]</our-cache>
```

To turn it off for one tag:

```cshtml
<our-cache update-cache-on-publish="false">[Your long running, expensive code here]</our-cache>
```

You can still clear it when one page is published by varying by that page's update date:

```cshtml
<our-cache update-cache-on-publish="false" vary-by="@Model.UpdateDate.ToString()">
    <p>@DateTime.Now</p>
</our-cache>
```

:::tip
On a busy site with many cached blocks and frequent publishing, clearing every cache on every publish can cost more than it saves. Think about how often your content changes before leaving it on everywhere.
:::
