---
title: Removed tag helpers
description: Tag helpers that are no longer in the package, and what to use instead.
removedIn: 2.0.0
---

These tag helpers were in version 1 of the package and have been removed.

## `<our-macro>`

`<our-macro>` rendered an Umbraco macro. It was removed in version 2.0.0, because Umbraco 14 removed macros.

Move the macro's markup and logic into a partial view, a view component or a block, and render that instead.
