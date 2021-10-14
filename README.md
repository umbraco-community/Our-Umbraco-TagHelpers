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
