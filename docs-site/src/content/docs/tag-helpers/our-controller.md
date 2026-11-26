---
title: our-controller
description: Posts a plain HTML form to an Umbraco surface controller, instead of Html.BeginUmbracoForm.
addedIn: 0.1.0
usage: attribute
---

`our-controller` and `our-action` go on a `<form>` to post it to an Umbraco [surface controller](https://docs.umbraco.com/umbraco-cms/fundamentals/code/creating-forms), as an alternative to `@Html.BeginUmbracoForm`. The tag helper adds the hidden `ufprt` field that tells Umbraco which controller and action to route the post to.

## Before and after

```cshtml title="Before"
@using (Html.BeginUmbracoForm("ContactForm", "Submit", FormMethod.Post, new { @id = "customerForm", @class = "needs-validation", @novalidate = "novalidate" }))
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
    <input type="submit" name="Submit" value="Submit" />
}
```

```cshtml title="After"
<form our-controller="ContactForm" our-action="Submit" method="post" id="customerForm" class="needs-validation" novalidate>
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
    <input type="submit" name="Submit" value="Submit" />
</form>
```

ASP.NET Core's own form tag helper adds the anti-forgery token, so you don't need to add one.

## Attributes

| Attribute | What it does |
|---|---|
| `our-controller` | The surface controller's name, with or without its `Controller` suffix (`ContactForm` or `ContactFormController`). |
| `our-action` | The action method to post to. |
| `umb-area` | The controller's area, if it's in one. |
| `our-route-val-*` | Extra route values passed to the action, one attribute each. `our-route-val-id="42"` passes `id` with the value `42`. |

Both `our-controller` and `our-action` are needed: without either, the form is rendered without the hidden field.
