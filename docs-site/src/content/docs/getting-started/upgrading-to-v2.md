---
title: Upgrading to version 2
description: What changes when you move a site from Our.Umbraco.TagHelpers 1.x to 2.x.
sidebar:
  order: 3
---

Version 2 supports Umbraco 16 and later, where the backoffice was rebuilt and macros were removed. Most tag helpers work exactly as they did in version 1. These are the changes to check in your views.

## Removed

- **`<our-macro>`** is gone, because Umbraco 14 removed macros. Move the macro's markup to a partial view or a block, and render that instead. See [Removed tag helpers](../../tag-helpers/removed/).

## Changed

- **`<our-edit-link>`** links to the new backoffice, so:
  - `content-id` is now `content-key`, and takes the content item's key (a GUID) instead of its integer ID.
  - `edit-url` is gone. The link always goes to `/umbraco/section/content/workspace/document/edit/{key}`.

## Check the release notes

Fixes and new options are listed with each version in the [release notes](../../release-notes/).
