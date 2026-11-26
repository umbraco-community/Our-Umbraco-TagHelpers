---
title: our-user-include
description: Shows or hides an element depending on the logged-in backoffice user's user groups.
addedIn: 0.5.0
usage: attribute
sidebar:
  label: our-user-include / exclude
---

`our-user-include` and `our-user-exclude` go on any element. They show or hide the element and its children depending on the user groups of the Umbraco backoffice user who's logged in. Give them a comma-separated list of user group **aliases**.

```cshtml
<div our-user-include="admin">Only users in the Administrators group see this.</div>
<div our-user-include="admin,editor">Only users in the Administrators or Editors group see this.</div>
<div our-user-include="*">Only logged-in backoffice users see this.</div>
<div our-user-include="?">Only visitors who aren't logged in to the backoffice see this.</div>

<div our-user-exclude="editor">Everyone except Editors sees this (including visitors who aren't logged in).</div>
<div our-user-exclude="?">Everyone except visitors who aren't logged in to the backoffice sees this.</div>
<div our-user-exclude="*">Everyone except logged-in backoffice users sees this.</div>
```

## Special groups

| Value | Means |
|---|---|
| `*` | Any logged-in backoffice user. |
| `?` | Anyone who isn't logged in to the backoffice. |

## Good to know

- `our-user-include` shows the element when the user is in **any** of the groups.
- `our-user-exclude` hides the element when the user is in **any** of the groups.
- Use the group's alias (such as `admin`, `editor` or `writer`), not its name.
- For website members rather than backoffice users, use [`our-member-include`](../our-member/).
