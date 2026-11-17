---
title: our-active-class
description: Adds a CSS class to a link when it points to the current page or one of its ancestors.
addedIn: 0.6.0
usage: attribute
---

`our-active-class` goes on an `<a>`. When the link points to the page being rendered, or to any page above it, the class in the attribute is added to the link's `class`.

```cshtml
@foreach (var item in Model.Root().Children)
{
    <a href="@item.Url()" class="nav-link" our-active-class="nav-link--active">@item.Name</a>
}
```

A navigation item stays active while one of its child or grandchild pages is being rendered. Links to other pages, or to children of the current page, don't get the class.

## On any element

Add `our-active-href` to use it on an element that isn't a link, such as the `<li>` around one:

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

## Attributes

| Attribute | What it does |
|---|---|
| `our-active-class` | The class to add when the link is active. |
| `our-active-href` | The URL to compare, for elements other than `<a>`. On an `<a>`, its `href` is used. |

## How links are compared

It works with and without culture domains (for example `/en/` and `/da/`), and ignores case, a trailing slash, the query string and the fragment.
