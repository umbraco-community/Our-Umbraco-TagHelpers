# Our Umbraco TagHelpers

A community project of C# ASP.NET TagHelpers for the Open Source CMS Umbraco

## Installing

Add the following Nuget Package to your Umbraco website project `Our.Umbraco.TagHelpers` with Visual Studio, Rider or with the dotnet CLI tool as follows when inside the directory with the .CSProj file for the Umbraco website project.

```
cd MyUmbracoProject
dotnet add package Our.Umbraco.TagHelpers
```

## Setup

With the Nuget package added you need to register the collection of TagHelpers for Razor views and partials to use them.
Browse to `/Views/_ViewImports.cshtml` in your Umbraco project and add the following line at the bottom

```cshtml
@addTagHelper *, Our.Umbraco.TagHelpers
```

## `<our-dictionary>`

This is a tag helper element `<our-dictionary>` that will use the current page's request Language/Culture to use a dictionary translation from the Umbraco translation section.

- Find dictionary key
- Find translation for Current Culture/Language of the page
- If no translation found see if we have fallback attribute set fallback-lang or umb-dictionary-fallback-lang
- Attempt to find dictionary item for that ISO language fallback
- No translation found - leave default/value inside tag untouched for final fallback

```cshtml
<h3><our-dictionary key="home">My Header</our-dictionary></h3>
<h3><our-dictionary key="home" fallback-lang="da-DK">My Header</our-dictionary></h3>
```

> **Note:** the translation is output as **HTML**, so markup in it is rendered: a translation of `Hello <strong>there</strong>` shows bold text. This is different from `@Umbraco.GetDictionaryValue("home")` in Razor, which HTML-encodes the value and shows the tags as text. Treat dictionary items like rich text: only let trusted users edit them, and use `@Umbraco.GetDictionaryValue(...)` instead when a translation should always be plain text.

## `our-if`

This is a tag helper attribute that can be applied to any DOM element in the razor template or partial. It will include its element and children on the page if the expression inside the `our-if` attribute evaluates to true.

### Simple Example

```cshtml
<div our-if="(DateTime.UtcNow.Minute % 2) == 0">This will only render during <strong>even</strong> minutes.</div>
<div our-if="(DateTime.UtcNow.Minute % 2) == 1">This will only render during <strong>odd</strong> minutes.</div>
```

### Example Before

```cshtml
@if (Model.ContentPickerThing != null)
{
    <a href="Model.ContentPickerThing.Url()" class="btn btn-action">
        <span>@Model.ContentPickerThing.Name</span>

        @if (Model.LinkMediaPicker != null)
        {
            <img src="@Model.LinkMediaPicker.Url()" class="img-circle" />
        }
    </a>
}
```

### Example After

```cshtml
<a our-if="Model.ContentPickerThing != null" href="@Model.ContentPickerThing?.Url()" class="btn btn-action">
    <span>@Model.ContentPickerThing.Name</span>
    <img our-if="Model.LinkMediaPicker != null" src="@Model.LinkMediaPicker?.Url()" class="img-circle" />
</a>
```

## `our-text-if`

This is a tag helper attribute for an **empty element**: it fills the element with the value in the attribute, or leaves the element out of the page when the value is empty. It's a shorter way to write an `our-if` that checks a value isn't empty.

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

- **Use it on an empty element.** Anything you put inside the element is never shown: it's replaced by the value, or left out with the element. For content to show when the value is empty, use [`<our-fallback>`](#our-fallback) instead.
- The value is HTML-encoded, as `@Model.Heading` is in Razor. Values that are already HTML, such as rich text (`IHtmlEncodedString`) or `IHtmlContent`, are output as HTML.
- It works with any value: `our-text-if="@Model.Value("subtitle")"` behaves the same, and a property with no value (or one that doesn't exist) leaves the element out.
- The element can be written self-closing (`<h1 ... />`); it's rendered with an end tag to hold the value.
## `<our-macro>`

> [!WARNING]
> The tag helper element `<our-macro>` has been removed in version 2.0.0 since Umbraco 14.0.0 deprecated the use of macros.

## BeginUmbracoForm Replacement

This is to make it easier to create a HTML `<form>` that uses an Umbraco SurfaceController and would be an alternative of using the `@Html.BeginUmbracoForm` approach. This taghelper runs against the `<form>` element along with these attributes `our-controller`and `our-action` to help generate a hidden input field of `ufprt` containing the encoded path that this form needs to route to. `our-controller` takes the controller's name with or without its `Controller` suffix (`ContactForm` or `ContactFormController`).

https://our.umbraco.com/Documentation/Fundamentals/Code/Creating-Forms/

### Before

```cshtml
@using (Html.BeginUmbracoForm("ContactForm", "Submit", FormMethod.Post, new { @id ="customerForm", @class = "needs-validation", @novalidate = "novalidate" }))
{
    @Html.ValidationSummary()

    <div class="input-group">
        <p>Name:</p>
        @Html.TextBoxFor(m => m.Name)
        @Html.ValidationMessageFor(m => m.Name)
    </div>
    <div>
        <p>Email:</p>
        @Html.TextBoxFor(m => m.Email)
        @Html.ValidationMessageFor(m => m.Email)
    </div>
    <div>
        <p>Message:</p>
        @Html.TextAreaFor(m => m.Message)
        @Html.ValidationMessageFor(m => m.Message)
    </div>
    <br/>
    <input type="submit" name="Submit" value="Submit" />
}
```

### After

```cshtml
<form our-controller="ContactForm" our-action="Submit" method="post" id="customerForm" class="fneeds-validation" novalidate>
    <div asp-validation-summary="All"></div>

    <div class="input-group">
        <p>Name:</p>
        <input asp-for="Name" />
        <span asp-validation-for="Name"></span>
    </div>
    <div>
        <p>Email:</p>
        <input asp-for="Email" />
        <span asp-validation-for="Email"></span>
    </div>
    <div>
        <p>Message:</p>
        <textarea asp-for="Message"></textarea>
        <span asp-validation-for="Message"></span>
    </div>
    <br/>
    <input type="submit" name="Submit" value="Submit" />
</form>
```

## `<our-lang-switcher>`

This tag helper element `<our-lang-switcher>` will create a simple unordered list of all languages and domains, in order to create a simple language switcher.
As this produces alot of HTML markup that is opionated with certain class names and elements, you may wish to change and control the markup it produces.

With this tag helper the child DOM elements inside the `<our-lang-switcher>` element is used as a Mustache templating language to control the markup.

### Example

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

<div class="lang-switcher">
    <div class="lang-switcher__item">
         <a href="https://localhost:44331/en" lang="en-US" hreflang="en-US" class="lang-switcher__link selected">English</a>
     </div>
    <div class="lang-switcher__item">
         <a href="https://localhost:44331/dk" lang="da-DK" hreflang="da-DK" class="lang-switcher__link ">dansk</a>
     </div>
</div>
```

If you do not specify a template and use `<our-lang-switcher />` it will use the following Mustache template

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

## `<our-svg>`

This tag helper element `<our-svg>` will read the file contents of an SVG file and output it as an inline SVG in the DOM.
It can be used in one of two ways, either by specifying the `src` attribute to a physcial static file served from wwwRoot or by specifying the `media-item` attribute to use a picked IPublishedContent Media Item.

### Basic usage:

```cshtml
<our-svg src="/assets/icon.svg" />
<our-svg media-item="@Model.Logo" />
```

### Advanced usage: \*(as of version 1.x.x)

Additional properties have been added to cache the output and also to ensure the SVG contains a viewbox property instead of the width & height properties to aid in making the vector image responsive within a parent HTML element.

```cshtml
<our-svg src="/assets/icon.svg" class="my-css-class" ensure-viewbox="true" cache="true" cache-minutes="180" ignore-appsettings="true" />
<our-svg media-item="@Model.Logo" class="my-css-class" ensure-viewbox="true" cache="true" cache-minutes="180" ignore-appsettings="true" />
```

- `class` - Allows for a CSS class upon the SVG element. This is a `string` value.
- `ensure-viewbox` - Enables the feature to "fix" the output SVG which always ensures the SVG utilises a viewbox rather than width & height. This is a `boolean` value.
- `cache` - Caches the SVG file's contents in Umbraco's runtime cache, so the file isn't read on every page load. This is a `boolean` value. When it's on the tag, it wins over the global `Cache` setting, so `cache="false"` turns caching off for that element even when it's on globally. See [When is an SVG cached?](#when-is-an-svg-cached) below.
- `cache-minutes` - How long (in minutes) a cached SVG is kept. When it isn't set, the global `CacheMinutes` is used (180 by default). This is an `integer` value.
- `ignore-appsettings` - Ignores the global `EnsureViewBox` and `Cache` settings for this element, so only the attributes on the tag apply. This is a `boolean` value.
- Any **other attributes**, such as `aria-hidden`, `aria-label`, `role`, `id`, `data-*`, `width` or `height`, are copied onto the inlined `<svg>`, replacing the file's own values. For example `<our-svg src="/assets/icon.svg" aria-hidden="true" />` for a decorative icon, or `role="img" aria-label="Company logo"` for a meaningful one.

### When is an SVG cached?

`cache` on the tag always decides when it's there. When it isn't, the global `Cache` setting decides, unless the tag has `ignore-appsettings="true"`, in which case the SVG isn't cached.

| `cache` on the tag | `ignore-appsettings` on the tag | Global `Cache` in appsettings | Cached? |
|---|---|---|---|
| not set | not set | `false` | no |
| not set | not set | `true` | **yes** |
| not set | `ignore-appsettings="true"` | `false` | no |
| not set | `ignore-appsettings="true"` | `true` | no |
| `cache="true"` | not set | `false` | **yes** |
| `cache="true"` | not set | `true` | **yes** |
| `cache="true"` | `ignore-appsettings="true"` | `false` | **yes** |
| `cache="true"` | `ignore-appsettings="true"` | `true` | **yes** |
| `cache="false"` | not set | `false` | no |
| `cache="false"` | not set | `true` | no |
| `cache="false"` | `ignore-appsettings="true"` | `false` | no |
| `cache="false"` | `ignore-appsettings="true"` | `true` | no |

Only the file's contents are cached, so `class` and `ensure-viewbox` still apply to each tag separately, even when several tags share one cached file.

### What is removed from the SVG

The SVG is inlined into the page, so anything in it that could run script is removed first:
- `<script>`, `<foreignObject>`, `<iframe>`, `<embed>` and `<object>` elements,
- event handler attributes such as `onload` and `onclick`,
- links that aren't internal (`#…`), web (`http`, `https`, `mailto`, relative) or, for images, image references, such as `javascript:` and `data:text/html`,
- `<set>` and `<animate>` elements that change a link or an event handler.

Everything else, including styles, gradients, internal references and animations, is kept as it is.

Media library SVGs are uploaded by backoffice users. To also stop unsafe SVGs from being uploaded at all (they can be opened directly from the media URL), add a file validator with Umbraco's [server-side file validation](https://docs.umbraco.com/umbraco-cms/run-in-production/security/serverside-file-validation).

### Global settings via appsettings.json

Applying any of the below configurations within your `appsettings.json` file will apply global settings to all elements using this tag helper. See the `ignore-appsettings` to override these global settings at element level. The values shown below are the hard-coded default values.

    {
      "Our.Umbraco.TagHelpers": {
        "OurSVG": {
          "EnsureViewBox": false,
          "Cache": false,
          "CacheMinutes": 180
        }
      }
    }

> **Note:** cached SVGs from the media library (`media-item`) are cleared when media is saved or changed, on every server of a load-balanced site. SVGs from `wwwroot` (`src`) stay cached until `cache-minutes` expires or the site restarts.

## `<our-fallback>`

This tag helper element `<our-fallback>` uses the same fallback mode logic that is only available on the `Value()` method of the `IPublishedContent` interface that uses a string for the property name to lookup. In addition if the fallback value from a language or ancestors is not available we are still able to fallback to the content inside the tag.

```cshtml
@* Current way *@
@Model.Value("Header", fallback:Fallback.ToLanguage)

<h3><our-fallback property="Header" mode="Fallback.ToLanguage" culture="da-DK">I do NOT have a DK culture variant of this property</our-fallback></h3>
<h3><our-fallback property="Header" mode="Fallback.ToAncestors">I do NOT have a Header property set on ANY parent and ancestors</our-fallback></h3>
```

## `<our-version>`

This tag helper element `<our-version>` prints out version number for a given Assembly name loaded into the current AppDomain or if none is given then the EntryAssembly version is displayed, which would be the Umbraco website project you are building.

```cshtml
<!-- Prints out the Website Project Assembly Version -->
<our-version />

<!-- Prints out the version number of a specific assembly loaded into Current AppDomain -->
<our-version assembly="Our.Umbraco.TagHelpers" />
```

## `<our-bytes>`

This tag helper element shows a number of bytes as a readable size, such as a download's file size.

```cshtml
<our-bytes media-item="@Model.Brochure" />
<our-bytes value="1152302" />
<our-bytes value="1234567" decimals="2" />
```

These render `1.1 MB` (for a 1,152,302-byte file), `1.1 MB` and `1.18 MB`, as plain text with no wrapping element.

- `value` - a number of bytes. When it's set, it's used instead of `media-item`.
- `media-item` - a media item (`IPublishedContent`) whose file size is shown: the `umbracoBytes` property Umbraco sets when a file is uploaded.
- `decimals` - the most decimals to show (default `1`). Trailing zeros are left out, so exactly 1 MB is `1 MB`.

Sizes go up in steps of 1024 (B, KB, MB, GB, TB, PB), and the number is formatted for the page's culture, so a Danish page shows `1,1 MB`. Nothing is output when there's no value, or it's negative.

## `our-member-include` and `our-member-exclude`

This is a tag helper attribute that can be applied to any DOM element in the razor template or partial. It will show or hide its element and children on the page when passing a comma seperated string of member groups that the current logged in member for the exclude or include variants.

There are two special Member Groups you can use:

- `*` - All anonymous users
- `?` - All authenticated users

```cshtml
<div our-member-include="Staff">Only members of Staff Member Group will see this.</div>
<div our-member-include="Staff,Admins">Only members of Staff OR Admins member group will see this.</div>
<div our-member-include="*">Only logged in members will see this.</div>
<div our-member-include="?">Only anonymous members will see this.</div>

<div our-member-exclude="Staff">Only Staff members can't see this (Including anonymous).</div>
<div our-member-exclude="?">Everyone except Anonymous members will see this.</div>
<div our-member-exclude="*">Everyone except who is authenticated will see this.</div>
```

## `our-user-include` and `our-user-exclude`

This is a tag helper attribute that can be applied to any DOM element in the razor template or partial. It will show or hide its element and children on the page when passing a comma seperated string of user groups that the current logged in Umbraco backoffice user is in, for the exclude or include variants.

There are two special User Groups you can use:

- `*` - All anonymous users
- `?` - All authenticated users

Use the alias of the User Group

```cshtml
<div our-user-include="admin">Only users in the Admin group will see this.</div>
<div our-user-include="admin,editor">Only users in the Admin or Editor user group will see this.</div>
<div our-user-include="*">Only logged in users will see this.</div>
<div our-user-include="?">Only anonymous users will see this.</div>

<div our-user-exclude="editor">Only Editor users can't see this (Including anonymous).</div>
<div our-user-exclude="?">Everyone except Anonymous users will see this.</div>
<div our-user-exclude="*">Everyone except who is authenticated will see this.</div>
```

## `<our-edit-link>`

This is a tag helper element which renders an edit link on the front end only if the current user is logged into umbraco and has access to the content section.

The link will open the current page in the umbraco backoffice. You can override the umbraco url if you are using a different url for the backoffice.

### Simple example

This is the most basic example. The link will render wherever you put it in the HTML.

```html
<our-edit-link>Edit</our-edit-link>
```

It will output a link link this, where 1057 is the id of the current page:

```html
<a href="/umbraco#/content/content/edit/1057">Edit</a>
```

### Use Default Styles example

If you add an attribute of `use-default-styles`, it will render the link fixed to the bottom left of the screen with white text and a navy blue background.

```html
<our-edit-link use-default-styles>Edit</our-edit-link>
```

### Change the edit url

Perhaps you have changed your umbraco path to something different, you can use the `edit-url` attribute to change the umbraco edit content url:

```html
<our-edit-link edit-url="/mysecretumbracopath#/content/content/edit/">Edit</our-edit-link>
```

### Open in a new tab

As the edit link is just an `a` tag, you can add the usual attributes like `target` and `class` etc.
If you want the edit link to open in a new tab, just add the `target="_blank"` attribute.

```html
<our-edit-link target="_blank">Edit</our-edit-link>
```

## `our-active-class`

This is a tag helper attribute that can be applied to `<a>` element in the razor template or partial. It will use the value inside the attribute and append it to the class attribute of the `<a>`.
It compares the link with the URLs of the page being rendered and its ancestors, so the class is added when the link points to the current page or any page above it. A navigation item stays active while one of its child or grandchild pages is being rendered; links to other pages, or to children of the current page, don't get the class.

This works with and without culture domains (for example `/en/` and `/da/`), and ignores case, a trailing slash, the query string and the fragment.

### Simple Example

```cshtml
@foreach (var item in Model.Root().Children)
{
    <a href="@item.Url()" class="nav-link" our-active-class="nav-link--active">@item.Name</a>
}
```

Alternatively you can use the `our-active-class` attribute in conjuction with `our-active-href` attribute to apply this to any HTML DOM element on the page.

```cshtml
<ul>
    @foreach (var item in Model.Root().Children)
    {
        <li our-active-href="@item.Url()" our-active-class="selected">
            <a href="@item.Url()" class="nav-link">@item.Name</a>
        </li>
    }
</ul>
```

## `<our-link>`

This tag helper element `<our-link>` will create a simple anchor tag using the Umbraco Multi Url Picker property editor that uses the C# Model `Umbraco.Cms.Core.Models.Link`

Note if the link set is an external link and you set the target of the link to be `_blank`, then the link will have the noopener attribute added to the link.

### Simple Example

```cshtml
<our-link link="@Model.ctaLink">
    <h2>Hi There</h2>
</our-link>
```

Alternatively if you use the `<our-link>` without child DOM elements then it will use the `Title` property of the link in the Multi Url Picker property editor to create the anchor tag.

```cshtml
<our-link link="@Model.ctaLink"></our-link>
```

With this tag helper the child DOM elements inside the `<our-link>` is wrapped with the `<a>` tag

### Making the link optional

You can also add `Fallback` for the link to become optional, if there is no link and there is child content, this content is still rendered, you can also replace the wrapping element by using `FallbackElement`.

```cshtml
<our-link link="@Model.ctaLink" Fallback="true" FallbackElement="div">
    <span>some text</span>
</our-link>

@*No link will result in*@
<div>
    <span>some text</span>
</div>

<our-link link="@Model.ctaLink" Fallback="true">
    <span>some text</span>
</our-link>

@*No link and no fallback HTML tag name specified will result in*@
<span>some text</span>
```

## `<our-cache>`

This tag helper element `<our-cache>` is a wrapper around the [DotNet CacheTagHelper](https://docs.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/built-in/cache-tag-helper?view=aspnetcore-6.0) - it operates in exactly the same way, with the same options as the DotNet CacheTagHelper, except, it is automatically 'not enabled', when you are in Umbraco Preview or Umbraco Debug mode.

### Without this tag helper

Essentially this is a convenience for setting

```cshtml
<cache enabled="!UmbracoContext.IsDebug && !UmbracoContext.InPreviewMode">[Your Long Running Expensive Code Here]</cache>
```

### With this tag helper

```cshtml
<our-cache>[Your Long Running Expensive Code Here]</our-cache>
```

### Clearing the Cache 'on publish'

The Umbraco convention with other Cache Helpers, eg Html.CachedPartial is to clear all memory caches whenever any item is published in the Umbraco Backoffice. By default the our-cache tag helper will do the same, however you can turn this part off on an individual TagHelper basis by setting update-cache-key-on-publish="false".

```cshtml
<our-cache>[Your Long Running Expensive Code Here]</our-cache>
```

is the same as:

```cshtml
<our-cache update-cache-on-publish="true">[Your Long Running Expensive Code Here]</our-cache>
```

But to turn it off:

```cshtml
<our-cache update-cache-on-publish="false">[Your Long Running Expensive Code Here]</our-cache>
```

(NB if you had a thousand tag helpers on your site, all caching large amounts of content, and new publishes to the site occurring every second - this might be detrimental to performance, so do think of the context of your site before allowing the cache to be cleared on each publish)

### Examples

All examples will skip the cache for Umbraco preview mode and debug mode, and evict cache items anytime Umbraco publishes content, media or dictionary items.

```cshtml
<our-cache expires-on="new DateTime(2025,1,29,17,02,0)">
     <p>@DateTime.Now - A set Date in time</p>
</our-cache>

<our-cache expires-after="TimeSpan.FromSeconds(120)">
     <p>@DateTime.Now - Every 120 seconds (2minutes)</p>
</our-cache>

<our-cache>
    <!-- A global navigation needs to be updated across entire site when phone number changes on homepage node -->
    <partial name="Navigation" />
</our-cache>

```

This example will turn off the automatic clearing of the tag helper cache if 'any page' is published, but still clear the cache if the individual page is published:

```cshtml
<our-cache update-cache-on-publish="false" vary-by="@Model.UpdateDate.ToString()">
     <p>@DateTime.Now - A set Date in time</p>
</our-cache>
```

## `<our-img>`

Introduced in version 1.x.x. This tag helper element `<our-img>` will produce performance orientated `<img>` or `<picture>`.

It can be used in one of two ways, either by specifying the `src` attribute to a physical static file served from wwwRoot or by specifying the `media-item` attribute to use a picked IPublishedContent Media Item.

### Properties

Properties appended with s, m, l, xl & xxl translate to screen widths defined by the default configuration or via the relevant properties in the `appsettings.json` (see the next section for more details).

- `src` - `string` based absolute or relative file URL. e.g. `/assets/image.jpg`.
- `media-item` - `MediaWithCrops` based class property from the view model. e.g. `Model.Image`.
- `alt` - Native alternative text property.
- `style` - Native style property.
- `class` - Native CSS class property.
- `abovethefold` - Set to `true` if the image typically appears on screen during inital page load to raise its page load lifecycle priority level.
- `width` - Native width property. Use in conjunction with the `height` property.
- `width--s` - Image width for small screens. Use in conjunction with the `height--s` property.
- `width--m` - Image width for medium screens. Use in conjunction with the `height--m` property.
- `width--l` - Image width for large screens. Use in conjunction with the `height--l` property.
- `width--xl` - Image width for extra large screens. Use in conjunction with the `height--xl` property.
- `width--xxl` - Image width for extra extra large screens. Use in conjunction with the `height--xxl` property.
- `height` - Native height property. Use in conjunction with the `width` property.
- `height--s` - Image height for small screens. Use in conjunction with the `width--s` property.
- `height--m` - Image height for medium screens. Use in conjunction with the `width--m` property.
- `height--l` - Image height for large screens. Use in conjunction with the `width--l` property.
- `height--xl` - Image height for extra large screens. Use in conjunction with the `width--xl` property.
- `height--xxl` - Image height for extra extra large screens. Use in conjunction with the `width--xxl` property.
- `cropalias` - Crop alias to be used by default.
- `cropalias--s` - Crop alias to be used on small screens.
- `cropalias--m` - Crop alias to be used on medium screens.
- `cropalias--l` - Crop alias to be used on large screens.
- `cropalias--xl` - Crop alias to be used on extra large screens.
- `cropalias--xxl` - Crop alias to be used on extra extra large screens.

### Global settings via appsettings.json

Applying any of the below configurations within your `appsettings.json` file will apply global settings to all elements using this tag helper. The values shown below are the hard-coded default values.

    {
      "Our.Umbraco.TagHelpers": {
        "OurIMG": {
          "MobileFirst": true, // Alternates between min-width & max-width media queries. When enabled, min-width is used.
          "MediaQueries": { // Window breakpoint widths in pixels
            "Small": 576,
            "Medium": 768,
            "Large": 992,
            "ExtraLarge": 1200,
            "ExtraExtraLarge": 1400
          },
          "UseNativeLazyLoading": true, // If enabled, loading="true" is used. If disabled, the 'src' property is replaced with 'data-src' which most lazy loading JavaScript libraries will interpret and lazy load the image.
          "LazyLoadCssClass": "lazyload", // If 'UseNativeLazyLoading' is disabled, the class property is given an additional class for JavaScript libraries to target. Note: 'lazyload' is used by the lazysizes library.
          "LazyLoadPlaceholder": "SVG", // If 'UseNativeLazyLoading' is disabled, the 'src' property is given either an empty SVG (if value is "SVG") or a lower quality version of the original image is used (if value is "LowQualityImage")
          "LazyLoadPlaceholderLowQualityImageQuality": 5, // If 'UseNativeLazyLoading' is disabled and 'LazyLoadPlaceholder' is "LowQualityImage", what image quality should be rendered. Numeric values 1-100 accepted.
          "ApplyAspectRatio": false, // If enabled, the aspect-ratio CSS style is applied to the style property, and the width & height properties are removed.
          "AlternativeTextMediaTypePropertyAlias": "alternativeText" // The media property alias to pull through the alt text value. If not found, the media title or filename will be used instead.
        }
      }
    }

### Examples

#### Example 1

This will produce a simple `<img>` tag with an alt tag either defined within the media properties in Umbraco, or auto generated based on the file name.

```cshtml
<our-img media-item="Model.Image" />
```

**Output:**

```cshtml
<img alt="Some alt text" width="1920" height="1280" src="/media/path/image.jpg?width=1920&amp;rnd=133087885756657361" loading="lazy" decoding="async" fetchpriority="low">
```

#### Example 2

This will produce an `<img>` tag with:

- an alt tag as defined within the tag,
- the width,
- the height (calculated by the aspect ratio of the orginal dimensions),
- no lazy loading with a higher fetch priority in the page load lifecycle.

```cshtml
<our-img media-item="Model.Image" width="200" alt="My custom alt text" abovethefold="true" />
```

**Output:**

```cshtml
<img alt="My custom alt text" width="200" height="133.33333333333331" src="/media/path/image.jpg?width=200&amp;rnd=133087885756657361" loading="eager" fetchpriority="high">
```

#### Example 3

This will produce an `<img>` tag with:

- JavaScript based lazy loading as instructed by the "UseNativeLazyLoading" appsetting set to `false`. This sets the image URL as `data-src` property instead of `src`. Additionally, the `src` property is instead given an empty SVG with the same aspect ratio. Finally, the CSS class `lazyload` has been added.
- the width,
- the height (calculated by the aspect ratio of the orginal dimensions).

```cshtml
<our-img media-item="article.ListingImage" width="200" />
```

**Output:**

```cshtml
<img alt="Some alt text" width="200" height="133.33333333333331" data-src="/media/uv2bljv1/meetup-organizers-at-codegarden.jpg?width=200&amp;rnd=133087885756657361" src="data:image/svg&#x2B;xml,%3Csvg xmlns=&#x27;http://www.w3.org/2000/svg&#x27; viewBox=&#x27;0 0 200 133.33333333333331&#x27;%3E%3C/svg%3E" class="lazyload" />
```

#### Example 4

```cshtml
<our-img media-item="Model.Image" width="200" width--s="400" width--m="600" cropalias="mobile" cropalias--m="desktop" />
```

**Output:**

```cshtml
<picture>
    <source srcset="/media/path/image.jpg?width=600&amp;height=300&amp;rnd=133087885756657361" media="(min-width: 768px)" width="600" height="300">
    <source srcset="/media/path/image.jpg?width=400&amp;height=400&amp;rnd=133087885756657361" media="(min-width: 576px)" width="400" height="400">
    <img alt="Some alt text" width="200" height="200" src="/media/path/image.jpg?width=200&amp;height=200&amp;rnd=133087885756657361" loading="lazy" decoding="async" fetchpriority="low">
</picture>
```

## `our-self-host`
This is a tag helper attribute that can be applied to any element using a `src` or `href` attribute in the razor template or partial. It will automatically download and self hosting of third party assets, like javascript, css or images. This was written by Soren Kottal for 24Days.in Umbraco Advent calendar 2022 article - https://24days.in/umbraco-cms/2022/static-assets-taghelper/

### Simple Example
```cshtml
<script src="https://unpkg.com/alpinejs@3.10.5/dist/cdn.min.js" our-self-host>
```

This will download the linked file to your local filesystem, and swap out the src attribute with a reference to the now locally hosted file.

### Folder location for downloaded files
By default the files will be saved in `/assets/`, and keep the folder path of the url. The root folder can be configured in appsettings.json, by adding a value at `Our.Umbraco.TagHelpers:OurSelfHost:RootFolder` specifying the desired rootfolder. The default value is `/assets/`.

You can further divide the files into folders, by adding a `folder` attribute to the script tag, eg:
```cshtml
<script src="https://unpkg.com/alpinejs@3.10.5/dist/cdn.min.js" our-self-host folder="libraries">
```
This will save the file in `/assets/libraries/alpinejs@3.10.5/dist/cdn.min.js`.

### Handling extensionless urls to files
In case the url is extensionless, like `https://unpkg.com/alpinejs`, you can add an `ext` attribute, for specifying the extension of the file, eg:
```cshtml
<script src="https://unpkg.com/alpinejs" our-self-host ext="js">
```
This will save the file as `/assets/alpinejs.js`, enabling eg. MIME types for .js files.

### Caching
The file is saved once, and never updated unless you manually remove the file. The lookup for the local file is cached in the Runtime Cache.

If a file couldn't be saved (see below), the tag uses the original URL and tries the download again after 5 minutes, so a short outage at the CDN doesn't leave the site linking to it until the site restarts. Once the file is saved, it's used from then on.

### Limits on what's downloaded
`our-self-host` is meant for URLs written in your views, such as the CDN links above, not for values that editors or visitors enter. To keep downloads in check:
- Only `http` and `https` URLs are downloaded, and the file must end up inside the root folder (a `folder` attribute such as `../..` isn't allowed).
- Redirects are followed (up to 5) only to URLs that pass the same checks, including `AllowedHosts`, and never from `https` to `http`.
- If a download fails, takes too long or is too large, the tag keeps the original URL instead of breaking the page, and a warning is logged. The download is tried again after 5 minutes.

These can be set in appsettings.json under `Our.Umbraco.TagHelpers:OurSelfHost` (the values shown are the defaults):

    {
      "Our.Umbraco.TagHelpers": {
        "OurSelfHost": {
          "RootFolder": "/assets",
          "AllowedHosts": [],
          "MaxFileSizeKilobytes": 1024,
          "DownloadTimeoutSeconds": 30
        }
      }
    }

- `AllowedHosts` - the hosts files may be downloaded from, for example `["unpkg.com", "cdn.jsdelivr.net"]`. When it's empty, any host is allowed; a URL on another host keeps pointing at the original.
- `MaxFileSizeKilobytes` - the largest file that's downloaded, in kilobytes (1024, 1 MB, by default). That's plenty for scripts, stylesheets, fonts and web-ready images; raise it if you self host bigger files such as large photos.
- `DownloadTimeoutSeconds` - how long to wait for a download.

## Demo site & cross-version tests

The package supports Umbraco 16, 17, 18 and 19 from a single NuGet package (`net9.0`, `net10.0` and `net11.0` builds).

### Demo site
`Our.Umbraco.TagHelpers.TestSite` is a demo site with a sample of every tag helper. It runs Umbraco 17 (LTS) by default, so it needs the .NET 10 SDK, and imports its content with uSync on first boot:

```bash
dotnet run --project Our.Umbraco.TagHelpers.TestSite
```

- Open https://localhost:44302 (if the browser rejects the certificate, run `dotnet dev-certs https --trust` once).
- The backoffice is at `/umbraco`; the login is the unattended install user in `appsettings.Development.json`.
- To try the user and member samples as someone other than the admin, the site creates demo people on first boot (a TestSite migration, not part of the package), using the logins under `TestSite:DemoPeople` in `appsettings.Development.json`:
  - an Editor user and a user who can only see the Media section (log in at `/umbraco`),
  - a member of the `Staff` member group (log in with the member login form on the home page).
- Pass `-p:UmbracoVersion=16|17|18|19` to build or run it against another supported Umbraco version, e.g. `dotnet run --project Our.Umbraco.TagHelpers.TestSite -p:UmbracoVersion=19`.
- Seeing "No published content"? Your local database was created before the uSync first-boot import was set up. Delete `Our.Umbraco.TagHelpers.TestSite/umbraco/Data/Umbraco.sqlite.db*` to get a fresh install with all the content, or import it from the uSync dashboard in the backoffice. The same goes for demo content added later (such as the "Fallback Demo" page under Page A): uSync only imports it on a fresh install or from the dashboard. The demo users and member are created on any boot.

### Cross-version tests
`tests/e2e` contains [Playwright](https://playwright.dev) tests that publish the demo site for each Umbraco version, start it with a fresh database and check every tag helper sample renders as expected. They run in GitHub Actions on every pull request (all versions on Linux, plus Umbraco 17 on Windows and macOS), and locally on Windows, macOS and Linux.

Prerequisites: Node.js LTS and the .NET runtimes for the versions you test (.NET 9 for Umbraco 16, .NET 10 for 17 and 18, .NET 11 for 19).

```bash
cd tests/e2e
npm ci                           # first time only
npx playwright install chromium  # first time only

npm test                                         # builds the library, then tests all versions
npm test -- specs/our-img.spec.ts                # a single spec
npm test -- specs/our-form.spec.ts -g "umb-area" # tests whose name matches
npm run test:package                             # the same tests against the packed NuGet package
npm run report                                   # HTML report, with screenshots and traces of failures
```

Choose the versions with the `UMBRACO_VERSIONS` environment variable:

| Shell | Command |
|---|---|
| bash / zsh (macOS, Linux, Git Bash) | `UMBRACO_VERSIONS=17,19 npm test` |
| PowerShell | `$env:UMBRACO_VERSIONS="17,19"; npm test` (clear it with `Remove-Item Env:UMBRACO_VERSIONS`) |
| Command Prompt | `set UMBRACO_VERSIONS=17,19 && npm test` |

Each run publishes the sites into `tests/e2e/.data/` with throwaway databases, so it never touches the demo site's own database. Every run starts freshly published sites on ports 5016–5019; if one of those ports is still in use (for example by a site from an interrupted run), Playwright stops with "is already used" — stop that process and run again. While iterating on specs only, set `E2E_REUSE_SITES=1` to reuse sites that are already running (they keep serving the views they were published with).

To step through tests in Playwright's UI mode, run `npm run build:library` and then `npx playwright test --ui` (with `UMBRACO_VERSIONS` set to the version you want).

`npm test` builds the package once before the sites are published (they publish in parallel and must not build the same project at the same time). Running `npx playwright test` directly also works, but a site refuses to start if the package build is missing or older than its source — run `npm run build:library` first.

`npm test` runs the sites against the `Our.Umbraco.TagHelpers` project. `npm run test:package` tests what would be published instead. It packs the library like `build.ps1`, then installs that `.nupkg` into every site and checks that each site loads the tag helpers from the package and picks up its appsettings schema, before running the same tests. It takes the same arguments and `UMBRACO_VERSIONS`. The package gets a unique prerelease version (such as `2.1.0-e2e.20261115093000`) because NuGet caches packages by version. To test a package you've already packed, set `E2E_PACKAGE_DIR` to the folder holding its `.nupkg`.

Pull requests and pushes to `v2/dev` run both modes in GitHub Actions. Each run packs the package once with a CI version, `<version>-ci.<run number>` (such as `2.1.0-ci.42`, which NuGet sorts below `2.1.0-rc1` and `2.1.0`). It attaches `Our.Umbraco.TagHelpers.<version>.nupkg` and its `.snupkg` (symbols) to the run as two unzipped artifacts, and the package jobs test that exact package.

### Trying a pull request's package
On the run's summary page in GitHub Actions, download `Our.Umbraco.TagHelpers.<version>.nupkg` from the Artifacts section into a folder, such as `./nuget-package`. Then use that folder as a package source in your site and install the version in the file name, e.g. `dotnet add package Our.Umbraco.TagHelpers --version 2.1.0-ci.42 --source ./nuget-package`.

### Packing the NuGet package
`build.ps1` packs the package into `build.out/`. It runs in Windows PowerShell or [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) (`pwsh ./build.ps1` on macOS and Linux). The version is the `<Version>` in `Our.Umbraco.TagHelpers/Our.Umbraco.TagHelpers.csproj`; pass `-Version` to override it, e.g. `pwsh ./build.ps1 -Version 2.1.0-rc1`.

To try a packed build in the demo site, pack it with its own version, then run the site against it:

```bash
pwsh ./build.ps1 -Version 2.1.0-local.1
dotnet run --project Our.Umbraco.TagHelpers.TestSite -p:TagHelpersSource=Package -p:TagHelpersPackageVersion=2.1.0-local.1
```

Use a new version for each new build of the package, because NuGet caches packages by version.

## Video 📺

[![How to create ASP.NET TagHelpers for Umbraco](https://user-images.githubusercontent.com/1389894/138666925-15475216-239f-439d-b989-c67995e5df71.png)](https://www.youtube.com/watch?v=3fkDs0NwIE8)

## Attribution

The logo for Our.Umbraco.TagHelpers is made by [Freepik](https://www.freepik.com) from [flaticon.com](https://www.flaticon.com)
