---
title: our-if
description: Renders an element and its children only when an expression is true.
addedIn: 0.1.0
usage: attribute
---

`our-if` goes on any element. The element and everything inside it is rendered only when the expression in the attribute is true.

```cshtml
<div our-if="(DateTime.UtcNow.Minute % 2) == 0">This only renders during <strong>even</strong> minutes.</div>
<div our-if="(DateTime.UtcNow.Minute % 2) == 1">This only renders during <strong>odd</strong> minutes.</div>
```

## Before and after

Without the tag helper, each condition is a Razor `@if` block:

```cshtml title="Before"
@if (Model.ContentPickerThing != null)
{
    <a href="@Model.ContentPickerThing.Url()" class="btn btn-action">
        <span>@Model.ContentPickerThing.Name</span>

        @if (Model.LinkMediaPicker != null)
        {
            <img src="@Model.LinkMediaPicker.Url()" class="img-circle" />
        }
    </a>
}
```

With `our-if`, the condition sits on the element it controls:

```cshtml title="After"
<a our-if="Model.ContentPickerThing != null" href="@Model.ContentPickerThing?.Url()" class="btn btn-action">
    <span>@Model.ContentPickerThing?.Name</span>
    <img our-if="Model.LinkMediaPicker != null" src="@Model.LinkMediaPicker?.Url()" class="img-circle" />
</a>
```

:::note
The element's other attributes are still evaluated when the condition is false, so use `?.` as above to avoid a null reference.
:::

To show a value only when it isn't empty, [`our-text-if`](../our-text-if/) is shorter.
