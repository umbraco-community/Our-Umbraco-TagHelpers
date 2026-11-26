---
title: <our-version>
description: Prints the version number of your website project, or of any loaded assembly.
addedIn: 0.2.0
usage: element
---

`<our-version>` prints a version number. With no attributes it's your website project's version (the entry assembly). With `assembly`, it's the version of that assembly, if it's loaded.

```cshtml
<!-- The website project's version -->
<our-version />

<!-- The version of an assembly loaded in the site -->
<our-version assembly="Our.Umbraco.TagHelpers" />
```

## Attributes

| Attribute | What it does |
|---|---|
| `assembly` | The name of a loaded assembly, such as `Umbraco.Core`. Nothing is rendered if no assembly has that name. |
