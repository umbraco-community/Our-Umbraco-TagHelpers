# Our Umbraco TagHelpers

A community package of ASP.NET Core Razor tag helpers for Umbraco CMS: translations, member-only content, responsive images, inline SVGs and more, written as HTML instead of C#. One NuGet package for Umbraco 16, 17, 18 and 19.

**📖 [Documentation: umbraco-community.github.io/Our-Umbraco-TagHelpers](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/)**

## Install

```bash
dotnet add package Our.Umbraco.TagHelpers
```

Then register the tag helpers by adding this line at the bottom of `Views/_ViewImports.cshtml`:

```cshtml
@addTagHelper *, Our.Umbraco.TagHelpers
```

See [Installation](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/getting-started/installation/) and [Configuration](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/getting-started/configuration/) for the details.

## An example

```cshtml
@* Rather than *@
@if (!string.IsNullOrWhiteSpace(Model.Heading))
{
    <h1>@Model.Heading</h1>
}

@* you can write *@
<h1 our-text-if="@Model.Heading" />
```

## The tag helpers

Each one links to its page in the docs, with every attribute, setting and example.

### Content and language

| Tag helper | What it does |
|---|---|
| [`<our-dictionary>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-dictionary/) | Outputs a dictionary translation for the current page's language, with fallbacks. |
| [`<our-fallback>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-fallback/) | Outputs a property using Umbraco's fallback modes, or the content inside the tag when there's no value. |
| [`<our-hreflang>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-hreflang/) | Outputs the hreflang alternate links for each language version of the current page. |
| [`<our-lang-switcher>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-lang-switcher/) | Renders a language switcher for every language and domain, with a Mustache template for the markup. |
| [`<our-link>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-link/) | Renders a link from a Multi URL Picker, with an optional fallback when no link is set. |

### Show or hide

| Tag helper | What it does |
|---|---|
| [`our-if`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-if/) | Renders an element and its children only when an expression is true. |
| [`our-text-if`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-text-if/) | Fills an empty element with a value, or leaves the element out when the value is empty. |
| [`our-member-include` / `our-member-exclude`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-member/) | Shows or hides an element depending on the logged-in member's member groups. |
| [`our-user-include` / `our-user-exclude`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-user/) | Shows or hides an element depending on the logged-in backoffice user's user groups. |

### Performance

| Tag helper | What it does |
|---|---|
| [`<our-img>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-img/) | Renders a fast, responsive `<img>` or `<picture>` from a media item or file, with crops and lazy loading. |
| [`<our-svg>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-svg/) | Inlines an SVG file or media item into the page, with caching, a viewBox fix and script removed. |
| [`<our-cache>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-cache/) | ASP.NET Core's cache tag helper, turned off in preview and debug, and cleared when content is published. |
| [`our-self-host`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-self-host/) | Downloads a third-party script, stylesheet or image once and serves it from your own site. |

### Site building

| Tag helper | What it does |
|---|---|
| [`our-controller`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-controller/) | Posts a plain HTML form to an Umbraco surface controller, instead of `Html.BeginUmbracoForm`. |
| [`our-active-class`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-active-class/) | Adds a CSS class to a link when it points to the current page or one of its ancestors. |
| [`<our-edit-link>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-edit-link/) | Shows a link to edit the current page in the backoffice, only to users who can edit content. |
| [`<our-bytes>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-bytes/) | Shows a number of bytes, or a media item's file size, as a readable size such as 1.1 MB. |
| [`<our-version>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-version/) | Prints the version number of your website project, or of any loaded assembly. |

Removed in version 2: `<our-macro>`, as Umbraco 14 removed macros. See [Removed tag helpers](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/removed/), and [Upgrading to version 2](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/getting-started/upgrading-to-v2/) if you're coming from 1.x.

## Supported versions

| Umbraco | .NET |
|---|---|
| 16 | .NET 9 |
| 17 (LTS) | .NET 10 |
| 18 | .NET 10 |
| 19 | .NET 11 |

Version 1.x supports earlier versions of Umbraco, from Umbraco 9. Its README is on the [`v1/dev` branch](https://github.com/umbraco-community/Our-Umbraco-TagHelpers/tree/v1/dev).

## Contributing

Issues, ideas and pull requests are welcome. Version 2 is developed on the `v2/dev` branch.

- [The demo site](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/contributing/demo-site/): a sample of every tag helper, on any supported Umbraco version (`dotnet run --project Our.Umbraco.TagHelpers.TestSite`).
- [Cross-version tests](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/contributing/cross-version-tests/): the unit tests and the Playwright suite that checks every sample on Umbraco 16 to 19.
- [Packing](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/contributing/packing/) and [writing the docs](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/contributing/writing-docs/).

## Video 📺

[![How to create ASP.NET TagHelpers for Umbraco](https://user-images.githubusercontent.com/1389894/138666925-15475216-239f-439d-b989-c67995e5df71.png)](https://www.youtube.com/watch?v=3fkDs0NwIE8)

## Attribution

The logo for Our.Umbraco.TagHelpers is made by [Freepik](https://www.freepik.com) from [flaticon.com](https://www.flaticon.com)
