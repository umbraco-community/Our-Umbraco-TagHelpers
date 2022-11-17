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

* Find dictionary key
* Find translation for Current Culture/Language of the page
* If no translation found see if we have fallback attribute set fallback-lang or umb-dictionary-fallback-lang
* Attempt to find dictionary item for that ISO language fallback
* No translation found - leave default/value inside tag untouched for final fallback

```cshtml
<h3><our-dictionary key="home">My Header</our-dictionary></h3>
<h3><our-dictionary key="home" fallback-lang="da-DK">My Header</our-dictionary></h3>
```

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

## `<our-macro>`
This tag helper element `<our-macro>` will render an Umbraco Macro Partial View and will use the current page/request for the Macro rendering & context.

If you wish, you can modify this behaviour and pass the context/content node that the Macro will render with using an optional attribute `content` on the `<our-macro>` tag and passing an `IPublishedContent` into the attribute. This allows the same Macro Partial View Macro code/snippet to work in various scenarios when the content node/context is changed.

Additionally custom Macro Parameters that can be passed through and consumed by Macro Partial Views are specified in the following way. The key/alias of the Macro Parameter must be prefixed with the following `bind:`

So to pass/set a value for the macro parameter `startNodeId` then I will need to set an attribute on the element as follows `bind:startNodeId`

```cshtml
<our-macro alias="ListChildrenFromCurrentPage" />
<our-macro alias="ListChildrenFromCurrentPage" Content="Model" />
<our-macro alias="ListChildrenFromCurrentPage" Content="Model.FirstChild()" />
<our-macro alias="ChildPagesFromStartNode" bind:startNodeId="umb://document/a878d58b392040e6ae9432533ac66ad9" />
```

## BeginUmbracoForm Replacement
This is to make it easier to create a HTML `<form>` that uses an Umbraco SurfaceController and would be an alternative of using the `@Html.BeginUmbracoForm` approach. This taghelper runs against the `<form>` element along with these attributes `our-controller`and `our-action` to help generate a hidden input field of `ufprt` containing the encoded path that this form needs to route to.

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

### Advanced usage: *(as of version 1.x.x)

Additional properties have been added to cache the output and also to ensure the SVG contains a viewbox property instead of the width & height properties to aid in making the vector image responsive within a parent HTML element. 
```cshtml
<our-svg src="/assets/icon.svg" class="my-css-class" ensure-viewbox="true" cache="true" cache-minutes="180" ignore-appsettings="true" />
<our-svg media-item="@Model.Logo" class="my-css-class" ensure-viewbox="true" cache="true" cache-minutes="180" ignore-appsettings="true" />
```

- `class` - Allows for a CSS class upon the SVG element. This is a `string` value.
- `ensure-viewbox` - Enables the feature to "fix" the output SVG which always ensures the SVG utilises a viewbox rather than width & height. This is a `boolean` value.
- `cache` - Enables the feature to cache the output at runtime level. This is a `boolean` value.
- `cache-minutes` - Defines the amount of time (in minutes) to cache the output. To be used in conjunction with the `cache` property. This is an `integer` value.
- `ignore-appsettings` - When enabled, the all settings appropiate to this tag helper which are defined within `appsettings.json` are completely ignored. For example, if global caching is enabled we can simply disable caching of individual elements (unless the `cache` property is `true`). This is a `boolean` value.

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

> **Note:** SVG caches are cleared on application restart, or by resaving the media in the media library.

    

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

## `our-member-include` and `our-member-exclude`
This is a tag helper attribute that can be applied to any DOM element in the razor template or partial. It will show or hide its element and children on the page when passing a comma seperated string of member groups that the current logged in member for the exclude or include variants.

There are two special Member Groups you can use:
* `*` - All anonymous users 
* `?` - All authenticated users

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
If the link inside the href attribute can be found by its route as a content node, if so then it will check with the current page being rendered if its the same node or an ancestor.

This allows for the navigation item to still have the class added to it when a child or grandchildren page is the currently page being rendered.

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



## Video 📺
[![How to create ASP.NET TagHelpers for Umbraco](https://user-images.githubusercontent.com/1389894/138666925-15475216-239f-439d-b989-c67995e5df71.png)](https://www.youtube.com/watch?v=3fkDs0NwIE8)


## Attribution
The logo for Our.Umbraco.TagHelpers is made by [Freepik](https://www.freepik.com) from [flaticon.com](https://www.flaticon.com)
