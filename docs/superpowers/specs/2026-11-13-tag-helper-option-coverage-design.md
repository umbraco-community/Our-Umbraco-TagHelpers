# Tag helper option coverage: design

**Status:** approved by Warren, 2026-11-13
**Builds on:** `2026-11-13-cross-version-playwright-tests-design.md` (the cross-version Playwright suite in `tests/e2e`)

## Intent

The cross-version suite covers every tag helper on its main path. Several per-tag options and user scenarios have no demo sample and no test, so a version-specific break in one of them (like the Umbraco 17 to 18 `ImgTagHelper` crash) would go unnoticed. This work adds a demo sample and a Playwright scenario, run on Umbraco 16, 17, 18 and 19, for each remaining option.

## Goals

| Tag helper | Covered by new samples and specs |
|---|---|
| `our-img` | `class`, `style`, `height` and `height--*`, and the `--l`/`--xl`/`--xxl` width and crop sizes produce the right markup and image URLs (and the URLs resolve) |
| `our-svg` | `class` is added; `cache`/`cache-minutes` serve the cached SVG after the file changes on disk, while uncached SVG shows the change; `ignore-appsettings` ignores the site's `OurSVG` settings |
| `our-cache` | publishing content clears the default cache; `update-cache-on-publish="false"` survives a publish of another page; `vary-by` gives new output when its value changes; the cache is skipped in debug (`?umbdebug=true`) and in preview |
| `our-form` | submitting the demo form posts to the surface controller: invalid input shows validation messages, valid input redirects back with a success message; a form with `umb-area` posts to a surface controller in that area |
| `our-user` | include/exclude for a user in the Editor group (not only the admin) |
| `our-member` | include/exclude for a member of a `Staff` member group |
| `our-edit-link` | hidden for a backoffice user without content section access |
| `our-fallback` | the other fallback modes: ancestors, and falling back to the tag content when no value is found |
| `our-self-host` | an `href` (stylesheet `<link>`) is rewritten to the local copy and that copy is served |

## Non-goals

- **Site-wide `our-img` appsettings** (lazy-load placeholder, JS lazy loading, `MobileFirst`, quality). They can't be changed per tag, and testing them would need a second site per version. Warren decided to rely on their defaults being covered.
- **Marking the active page with `our-active-class`.** That's a bug (issue #97, culture domains). It stays a `test.fixme`. This work only links the issue from the fixme comment.

## Constraints

- Same four sites (one per Umbraco version) and the same `npm test` entry point. No extra sites.
- The demo must keep working on a fresh install: content comes from uSync, and users, members and groups from the TestSite migration. No database or password hashes are committed. Nothing is added to the shipped package.
- Tests may run in parallel across spec files. Anything that publishes content or changes files on disk must not break another spec's assertions.
- Work happens on `v2/feature/multi-version-support`, with granular commits (one per tag helper where practical) and no `Co-Authored-By` trailer. Nothing is pushed.

## Approach

**Chosen (Warren, 2026-11-13):** a package migration in the TestSite project (not in the shipped package) creates the demo people on first boot: the user group, users, member group and member. Passwords are set at runtime through Umbraco's services, so no hashes are committed. Credentials sit in `appsettings.Development.json` next to the existing unattended admin login. The setup project (`specs/backoffice.setup.ts`) logs each identity in and saves a session for it (storage state), like the admin one today. Publishing and preview, which happen *during* the cache tests, use Umbraco's Management API from the tests.

**Considered:**
- *Creating them from the tests through the Management API.* Works on 16–19 (probed), but the demo backoffice then has no users or members for someone trying the samples by hand. Replaced by the migration.
- *uSync.* Free uSync has no user or member handlers. `uSync.PeopleEdition` does (disabled in the Default set until enabled), but its `license.txt` is the paid uSync.Complete agreement: a 60-day trial, then a licence for continued use. That doesn't fit an open-source repo and CI. Rejected.
- *Driving the backoffice UI.* Slow, and the UI changes between 16 and 19. Rejected.

## Design decisions

- **Demo people**, created by the TestSite migration on first boot (each skipped if it already exists):
  - An Editor user in the built-in `editor` group.
  - A user in a new "Demo No Content" user group (alias `demoNoContent`) with only the Media section.
  - A `Staff` member group and a member in it, of the default `Member` type.
  - Emails and one shared password come from `appsettings.Development.json` (`TestSite:DemoPeople`). Without that config the migration creates nothing, so nothing like it reaches other environments.
- **Sessions:** setup logs in the two users through the backoffice login and the member through the demo's member login form, and saves a session for each.
- **Publishing:** a test helper republishes a named content node through the Management API, using the admin session. Specs that publish run serially with the other cache specs (one file, `test.describe.configure({ mode: 'serial' })`). Only the `our-cache` spec reads cache timestamps, so a publish doesn't disturb other specs.
- **Demo sample fixes found along the way:**
  - In the user sample, "Everyone except Admin or Editor" has `our-user-exclude="editor"`; it should be `admin,editor`.
  - The user and member samples describe `*` and `?` the wrong way round. In the code, `?` means anonymous and `*` means logged in.
- **`our-svg` caching:** the spec changes an SVG file on disk inside the published site (`.data/vN/app/wwwroot/...`). It uses a file only that spec touches.
- **`our-svg` `ignore-appsettings`:** the demo `appsettings.json` gets an `OurSVG` setting whose effect is visible in the markup (for example `EnsureViewBox: true`). One sample with `ignore-appsettings` shows that it isn't applied.
- **`our-form` area:** a second demo surface controller marked `[PluginController("…")]` (which puts it in that area), with a form sample using `umb-area`. `ContactFormController` sets a `TempData` success message, which the form sample shows.

## Left to the builder

- Exact service calls in the migration. If an API differs between 16 and 19, use a per-version compile symbol set from `UmbracoVersion` in the TestSite project.
- How to enter preview mode from a test. If it can't be driven reliably on all four versions, cover the debug skip and leave preview as a documented `test.fixme`.
- Which page and property the `our-fallback` ancestors sample uses.
- Sample wording and layout. Follow the existing table and section style.
