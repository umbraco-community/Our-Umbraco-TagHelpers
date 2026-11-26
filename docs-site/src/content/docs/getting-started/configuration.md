---
title: Configuration
description: Global appsettings.json options for our-svg, our-img and our-self-host, with their defaults.
sidebar:
  order: 2
---

Three tag helpers read global options from `appsettings.json`, under the `Our.Umbraco.TagHelpers` section. Every option has a default, so you only add the ones you want to change.

## IntelliSense in appsettings.json

The package ships a JSON schema for these options. When you build the site, Umbraco adds it to the site's `appsettings-schema.json`, so editors that read that schema (Visual Studio, Rider and VS Code) suggest and describe the options as you type.

## All options with their defaults

```json title="appsettings.json"
{
  "Our.Umbraco.TagHelpers": {
    "OurSVG": {
      "EnsureViewBox": false,
      "Cache": false,
      "CacheMinutes": 180,
      "ScopeStyles": false,
      "UniqueIds": false
    },
    "OurIMG": {
      "MobileFirst": true,
      "MediaQueries": {
        "Small": 576,
        "Medium": 768,
        "Large": 992,
        "ExtraLarge": 1200,
        "ExtraExtraLarge": 1400
      },
      "UseNativeLazyLoading": true,
      "LazyLoadCssClass": "lazyload",
      "LazyLoadPlaceholder": "SVG",
      "LazyLoadPlaceholderLowQualityImageQuality": 5,
      "ApplyAspectRatio": false,
      "AlternativeTextMediaTypePropertyAlias": "alternativeText"
    },
    "OurSelfHost": {
      "RootFolder": "/assets",
      "AllowedHosts": [],
      "MaxFileSizeKilobytes": 1024,
      "DownloadTimeoutSeconds": 30
    }
  }
}
```

## OurSVG

Used by [`<our-svg>`](../../tag-helpers/our-svg/). An attribute on the tag wins over these, and `ignore-appsettings="true"` ignores them for one tag.

| Option | Default | What it does |
|---|---|---|
| `EnsureViewBox` | `false` | Replaces the SVG's `width` and `height` with a `viewBox`, so it scales with its container. |
| `Cache` | `false` | Keeps each SVG's contents in Umbraco's runtime cache, so the file isn't read on every request. |
| `CacheMinutes` | `180` | How long a cached SVG is kept, in minutes. |
| `ScopeStyles` | `false` | Scopes each SVG's `<style>` blocks to that SVG, so class names such as `.st0` from two SVGs don't restyle each other. See [Scoped styles](../../tag-helpers/our-svg/#scoped-styles). |
| `UniqueIds` | `false` | Gives the ids in each SVG a prefix of their own and updates the references to them, so SVGs on one page don't share gradients, clip paths or filters. See [Unique ids](../../tag-helpers/our-svg/#unique-ids). |

## OurIMG

Used by [`<our-img>`](../../tag-helpers/our-img/).

| Option | Default | What it does |
|---|---|---|
| `MobileFirst` | `true` | Writes `min-width` media queries when on, `max-width` when off. |
| `MediaQueries` | 576, 768, 992, 1200, 1400 | The screen widths, in pixels, that the `--s`, `--m`, `--l`, `--xl` and `--xxl` attributes use. |
| `UseNativeLazyLoading` | `true` | Uses `loading="lazy"`. When off, the image URL goes in `data-src` for a JavaScript lazy loader instead. |
| `LazyLoadCssClass` | `lazyload` | With native lazy loading off, the class added for the JavaScript library to find. `lazyload` suits lazysizes. |
| `LazyLoadPlaceholder` | `SVG` | With native lazy loading off, what goes in `src` until the image loads: `SVG` (an empty SVG of the same shape) or `LowQualityImage`. |
| `LazyLoadPlaceholderLowQualityImageQuality` | `5` | The quality, 1 to 100, of the `LowQualityImage` placeholder. |
| `ApplyAspectRatio` | `false` | Writes an `aspect-ratio` style and leaves out `width` and `height`. |
| `AlternativeTextMediaTypePropertyAlias` | `alternativeText` | The media property that holds alt text. Without it, the media's name or file name is used. |

## OurSelfHost

Used by [`our-self-host`](../../tag-helpers/our-self-host/).

| Option | Default | What it does |
|---|---|---|
| `RootFolder` | `/assets` | The folder, under `wwwroot`, that downloaded files are saved in. |
| `AllowedHosts` | `[]` | Hosts that files may be downloaded from, such as `["unpkg.com", "cdn.jsdelivr.net"]`. Empty allows any host. |
| `MaxFileSizeKilobytes` | `1024` | The largest file that's downloaded, in kilobytes. Larger files keep the original URL. |
| `DownloadTimeoutSeconds` | `30` | How long to wait for a download. A slower download keeps the original URL. |
