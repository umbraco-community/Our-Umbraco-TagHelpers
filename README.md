# Our Umbraco TagHelpers
A community project of C# ASP.NET TagHelpers for the Open Source CMS Umbraco


## Installing
Add the following Nuget Package to your Umbraco website project `Our.Umbraco.TagHelpers` with Visual Studio, Rider or with the dotnet CLI tool as follows when inside the directory with the .CSProj file for the Umbraco website project.

```
cd MyUmbracoProject
dotnet add package Our.Umbraco.TagHelpers`
```

## Setup

With the Nuget package added you need to register the collection of TagHelpers for Razor views and partials to use them.
Browse to `/Views/_viewStart.cshtml` in your Umbraco project and add the followign line at the bottom
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
This tag helper element `<our-umbraco>` will render an Umbraco Macro Partial View and will use the current page/request for the Macro rendering & context.

If you wish, you can modify this behaviour and pass the context/content node that the Macro will render with using an optional attribute `content` on the `<our-umbraco>` tag and passing an `IPublishedContent` into the attribute. This allows the same Macro Partial View Macro code/snippet to work in various scenarios when the content node/context is changed.

Additionally custom Macro Parameters that can be passed through and consumed by Macro Partial Views are specified in the following way. The key/alias of the Macro Parameter must be prefixed with the following `our-macro-`

So to pass/set a value for the macro parameter `startNodeId` then I will need to set an attribute on the element as follows `our-macro-startNodeId`

```cshtml
<our-macro alias="ListChildrenFromCurrentPage" />
<our-macro alias="ListChildrenFromCurrentPage" content="Model" />
<our-macro alias="ListChildrenFromCurrentPage" content="Model.FirstChild()" />
<our-macro alias="ChildPagesFromStartNode" our-macro-startNodeId="umb://document/4d39ea8a077949dbb2d80c9f0c7545" />
```
