---
title: Demo site
description: Run the demo site, with a sample of every tag helper, against any supported Umbraco version.
sidebar:
  order: 1
---

`Our.Umbraco.TagHelpers.TestSite` is a demo site with a sample of every tag helper. It runs Umbraco 17 (LTS) by default, so it needs the .NET 10 SDK, and imports its content with uSync on first boot.

```bash
dotnet run --project Our.Umbraco.TagHelpers.TestSite
```

- Open `https://localhost:44302`. If the browser rejects the certificate, run `dotnet dev-certs https --trust` once.
- The backoffice is at `/umbraco`. The login is the unattended install user in `appsettings.Development.json`.
- To try the user and member samples as someone other than the admin, the site creates demo people on first boot (a TestSite migration, not part of the package), using the logins under `TestSite:DemoPeople` in `appsettings.Development.json`:
  - an Editor user and a user who can only see the Media section (log in at `/umbraco`),
  - a member of the `Staff` member group (log in with the member login form on the home page).

## Another Umbraco version

Pass `-p:UmbracoVersion=16|17|18|19` to build or run the site against another supported version:

```bash
dotnet run --project Our.Umbraco.TagHelpers.TestSite -p:UmbracoVersion=19
```

## "No published content"?

Your local database was created before the uSync first-boot import was set up. Delete `Our.Umbraco.TagHelpers.TestSite/umbraco/Data/Umbraco.sqlite.db*` for a fresh install with all the content, or import it from the uSync dashboard in the backoffice.

The same goes for demo content added later (such as the "Fallback Demo" page under Page A): uSync only imports it on a fresh install or from the dashboard. The demo users and member are created on any boot.

## Adding a sample

Every sample sits in a `<section data-sample="name">` in `Views/Home.cshtml` or `Views/Page.cshtml`, which the [cross-version tests](../cross-version-tests/) find it by. A new tag helper option gets a sample and a test. Content the tests need goes in uSync (`uSync/site/`), and people go in the TestSite migration, never only in a local database.
