# Our Umbraco TagHelpers

**Less C# in your Razor, more HTML.** A community package of ASP.NET Core tag helpers for Umbraco CMS: translations, member-only content, responsive images, inline SVGs, forms and more, written as HTML. One NuGet package for Umbraco 16, 17, 18 and 19.

**📖 [Full documentation: umbraco-community.github.io/Our-Umbraco-TagHelpers](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/)**

## Install

```bash
dotnet add package Our.Umbraco.TagHelpers
```

Then add this line at the bottom of `Views/_ViewImports.cshtml`:

```cshtml
@addTagHelper *, Our.Umbraco.TagHelpers
```

## What's in it

### Content and language

**[`<our-dictionary>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-dictionary/)**: a dictionary translation for the current page's language, with fallbacks.

```cshtml
<h3><our-dictionary key="home" fallback-lang="da-DK">My Header</our-dictionary></h3>
```

**[`<our-fallback>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-fallback/)**: a property with Umbraco's fallback modes, or the content inside the tag when there's no value.

```cshtml
<h3><our-fallback property="Header" mode="Fallback.ToAncestors">Welcome</our-fallback></h3>
```

**[`<our-hreflang>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-hreflang/)**: the `hreflang` alternate links for each language version of the page, for search engines.

**[`<our-lang-switcher>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-lang-switcher/)**: a language switcher for every language and domain, with your own markup.

**[`<our-link>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-link/)**: a link from a Multi URL Picker, with an optional fallback when no link is set.

### Show or hide

**[`our-if`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-if/)** and **[`our-text-if`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-text-if/)**: render an element only when it's needed.

```cshtml
<div our-if="Model.ShowBanner">...</div>
<h1 our-text-if="@Model.Heading" />   @* left out when the heading is empty *@
```

**[`our-member-include` / `our-member-exclude`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-member/)** and **[`our-user-include` / `our-user-exclude`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-user/)**: show or hide content by member group or backoffice user group.

```cshtml
<div our-member-include="Staff">Only members of the Staff member group see this.</div>
<div our-user-include="admin,editor">Only users in the Administrators or Editors group see this.</div>
```

### Performance

**[`<our-img>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-img/)**: a fast, responsive `<img>` or `<picture>` from a media item, with crops, sizes per screen width and lazy loading.

```cshtml
<our-img media-item="Model.Image" width="200" width--m="600" cropalias="mobile" cropalias--m="desktop" />
```

**[`<our-svg>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-svg/)**: inlines an SVG file or media item, with caching and script removed, so you can style it with CSS.

```cshtml
<our-svg media-item="@Model.Logo" role="img" aria-label="Company logo" />
```

**[`<our-cache>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-cache/)**: ASP.NET Core's cache tag helper, turned off in preview and debug, and cleared when content is published.

**[`our-self-host`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-self-host/)**: downloads a third-party script, stylesheet or image once and serves it from your own site.

```cshtml
<script src="https://unpkg.com/alpinejs@3.10.5/dist/cdn.min.js" our-self-host></script>
```

### Site building

**[`our-controller`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-controller/)**: posts a plain HTML form to a surface controller, instead of `Html.BeginUmbracoForm`.

```cshtml
<form our-controller="ContactForm" our-action="Submit" method="post">...</form>
```

**[`our-active-class`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-active-class/)**: adds a CSS class to navigation links for the current page and its ancestors.

**[`<our-edit-link>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-edit-link/)**: an "Edit" link to the page in the backoffice, shown only to users who can edit content.

**[`<our-bytes>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-bytes/)**: a file size such as `1.1 MB`, from a number of bytes or a media item.

**[`<our-version>`](https://umbraco-community.github.io/Our-Umbraco-TagHelpers/tag-helpers/our-version/)**: the version number of your site, or of any loaded assembly.

## Supported versions

| Umbraco | .NET |
|---|---|
| 16 | .NET 9 |
| 17 (LTS) | .NET 10 |
| 18 | .NET 10 |
| 19 | .NET 11 |

Version 1.x supports earlier versions of Umbraco, from Umbraco 9.

## Open source

Our Umbraco TagHelpers is a community project, built by people across the Umbraco community. Issues, ideas and pull requests are welcome on [GitHub](https://github.com/umbraco-community/Our-Umbraco-TagHelpers).
