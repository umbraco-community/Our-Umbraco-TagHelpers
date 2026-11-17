---
title: Installation
description: Add Our.Umbraco.TagHelpers to an Umbraco 16, 17, 18 or 19 site and register the tag helpers.
sidebar:
  order: 1
---

Our.Umbraco.TagHelpers is one NuGet package that works on Umbraco 16, 17, 18 and 19. Install it, register it in `_ViewImports.cshtml`, and every tag helper is ready to use in your views.

## Install the package

From the folder with your Umbraco website's `.csproj`, run:

```bash
cd MyUmbracoProject
dotnet add package Our.Umbraco.TagHelpers
```

You can also add it with the NuGet package manager in Visual Studio or Rider.

## Register the tag helpers

Open `Views/_ViewImports.cshtml` and add this line at the bottom:

```cshtml title="Views/_ViewImports.cshtml"
@addTagHelper *, Our.Umbraco.TagHelpers
```

Every view and partial in the site can now use the tag helpers. Try one:

```cshtml
<h1 our-text-if="@Model.Value("heading")" />
```

## Supported versions

Version 2 of the package supports these Umbraco versions from the same package:

| Umbraco | .NET | Notes |
|---|---|---|
| 16 | .NET 9 | |
| 17 | .NET 10 | Long-term support (LTS) |
| 18 | .NET 10 | |
| 19 | .NET 11 | Release candidate |

Version 1.x of the package was built for earlier versions of Umbraco, starting with Umbraco 9.

## Next steps

- Browse [every tag helper](../../tag-helpers/).
- Set [global options](../configuration/) for `<our-svg>`, `<our-img>` and `our-self-host` in `appsettings.json`.
- Moving from version 1? Read [Upgrading to version 2](../upgrading-to-v2/).
