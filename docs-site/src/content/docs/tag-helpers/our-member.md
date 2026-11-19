---
title: our-member-include
description: Shows or hides an element depending on the logged-in member's member groups.
addedIn: 0.3.0
usage: attribute
sidebar:
  label: our-member-include / exclude
---

`our-member-include` and `our-member-exclude` go on any element. They show or hide the element and its children depending on the member groups of the member logged in to the website. Give them a comma-separated list of member group names.

```cshtml
<div our-member-include="Staff">Only members of the Staff member group see this.</div>
<div our-member-include="Staff,Admins">Only members of Staff OR Admins see this.</div>
<div our-member-include="*">Only logged-in members see this.</div>
<div our-member-include="?">Only visitors who aren't logged in see this.</div>

<div our-member-exclude="Staff">Everyone except Staff members sees this (including visitors who aren't logged in).</div>
<div our-member-exclude="?">Everyone except visitors who aren't logged in sees this.</div>
<div our-member-exclude="*">Everyone except logged-in members sees this.</div>
```

## Special groups

| Value | Means |
|---|---|
| `*` | Any logged-in member. |
| `?` | Anyone who isn't logged in. |

## Good to know

- `our-member-include` shows the element when the member is in **any** of the groups.
- `our-member-exclude` hides the element when the member is in **any** of the groups.
- On one element, exclude is checked first.
- For Umbraco backoffice users rather than members, use [`our-user-include`](../our-user/).
