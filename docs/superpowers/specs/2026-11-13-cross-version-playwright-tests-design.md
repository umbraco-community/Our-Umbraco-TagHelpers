# Cross-version tag helper tests & LTS demo site — design

Date: 2026-11-13
Branch: `v2/dev`

## Intent

The v2 package ships one `.nupkg` that supports Umbraco 16, 17, 18 and 19
(multi-targeted `net9.0` / `net10.0` / `net11.0`). A manual run on 2026-11-13
showed that "it builds" is not enough: the `net10.0` DLL compiled against 17
crashed at runtime on 18 (`MissingMethodException` in `ImgTagHelper`), which no
build or unit test caught.

We want an automated check that **every tag helper renders as expected on every
supported Umbraco version**, runnable:

- locally on demand (before a release), and
- in GitHub Actions on pull requests.

Alongside this, the repo's demo site (TestSite) moves to the current LTS,
**Umbraco 17**, so anyone cloning `v2` gets a working demo out of the box.

## Goals

- One command (`npx playwright test`) builds, boots and tests all four versions.
- One spec per tag helper sample; each test runs against each version, so
  failures read as e.g. `our-img › v18`.
- Exercise the real runtime binding: the site runs on Umbraco N while the
  package DLL is the one compiled for that TFM (catches 17-vs-18 style breaks).
- Logged-in scenarios covered: backoffice user (edit link, user include/exclude)
  and site member (member include/exclude).
- Clean, deterministic runs: fresh database per version per run.
- Failures produce Playwright's HTML report with screenshot + trace.
- Demo site works on a fresh clone with `dotnet run` on Umbraco 17.

## Non-goals

- Pixel/screenshot comparison across versions (backoffice/browser rendering
  differences would make it brittle). Assertions are on DOM/attributes/text.
- Testing the Umbraco backoffice itself.
- Replacing the NUnit unit tests.
- Publishing or testing the `.nupkg` from a feed (see "Approaches" decision on
  ProjectReference).

## Anti-goals (would be a failure even if it "works")

- Flaky tests (e.g. asserting the `our-if` even-minute sample or `our-cache`
  timestamps as fixed values).
- Per-version copies of the demo site checked into the repo.
- Tests that silently pass when a sample renders nothing.
- Changing the user's real TestSite database or any file outside the repo.

## Constraints (found during the manual run)

| Constraint | Consequence |
|---|---|
| Each Umbraco major needs one .NET: 16→net9.0, 17/18→net10.0, 19→net11.0 | Sites for 17 and 18 share a TFM, so they need separate restore/obj folders |
| Umbraco 17+ removed runtime Razor compilation | TestSite must set `RazorCompileOnBuild=true` (it uses `SourceCodeManual` models, so this is correct anyway) |
| `BlockListItem.ContentUdi` removed in 18 | `Views/Partials/blocklist/default.cshtml` checks `block?.Content` instead |
| Umbraco 18+ can't migrate a 16 DB directly | Use a **fresh unattended install** per version + uSync import, never DB migration |
| uSync defaults to `uSync/v14/`; files live in `uSync/v16/` | Configure uSync folders explicitly; import on first boot |
| DB is git-ignored | Fresh clones rely on `ImportOnFirstBoot` too — same mechanism as tests |
| `UseHttps` defaults to `true` from 17 | Tests run over HTTP with `Umbraco__CMS__Global__UseHttps=false` (env var, not committed config) |
| Umbraco 19 splits single media pickers into `Umbraco.MediaPicker.Single`; `MediaPicker3` always returns `IEnumerable<MediaWithCrops>` | uSync data from 17 yields a list on 19; the `our-svg` sample must not depend on `Model.Logo` being a single item |
| Logo media file `wwwroot/media/rtoiuk20/face-savoring-food.svg` missing from repo | Add it |
| `our-self-host` downloads from unpkg.com | Network dependency; acceptable in CI, test asserts the rewrite not the download content |
| Local machine has no .NET 9 runtime | Version selection via env var so a dev can run a subset |
| `localhost` cookies are shared across ports | Each version uses its own Playwright browser context (default per test) — no cross-site cookie bleed |

## Approaches

1. **Playwright Test (TypeScript), one Playwright project per Umbraco version** —
   *chosen*. Built-in `webServer` array boots all four sites and waits for them;
   HTML report, traces and screenshots for free; same command locally and in CI.
   Cost: adds Node + `package.json` to a .NET repo.
2. Playwright for .NET inside the NUnit project — stays in C#, but no
   `webServer` equivalent (we'd hand-roll booting four Umbraco sites) and weaker
   reporting.
3. Plain HTTP + HTML assertions, no browser — fastest, but backoffice login is
   an OpenIddict PKCE flow that is painful without a browser, and nothing to
   look at when it fails.

Picked by Warren: **1**.

## Design

### 1. TestSite becomes version-switchable (default 17 LTS)

`Our.Umbraco.TagHelpers.TestSite.csproj` gains an `UmbracoVersion` property
(default `17`) that selects TFM and package versions:

| `UmbracoVersion` | TFM | Umbraco.Cms | uSync |
|---|---|---|---|
| 16 | net9.0 | 16.5.1 | 16.1.0 |
| 17 (default) | net10.0 | 17.8.1 | 17.6.2 |
| 18 | net10.0 | 18.3.1 | 18.3.1 |
| 19 | net11.0 | 19.0.0-rc1 | 19.0.0-rc1 |

A `Directory.Build.props` **inside the TestSite folder only** sets
`BaseIntermediateOutputPath`/`BaseOutputPath` to `obj/u$(UmbracoVersion)/` and
`bin/u$(UmbracoVersion)/` so the four variants never share restore state. It
must not affect the library or test projects.

The TestSite keeps a **ProjectReference** to the library. Because the library is
multi-targeted and built with its own package references, the site on
Umbraco 18 runs the library's `net10.0` build compiled against 17.0.0 — the same
binary a NuGet consumer gets. (Packing to a local feed adds packaging coverage
but slows every run; not in scope.)

Other TestSite changes:
- `RazorCompileOnBuild` / `RazorCompileOnPublish` → `true`.
- Blocklist partial: `ContentUdi` → `Content` null check.
- uSync files move from `uSync/v16/` to a version-neutral `uSync/site/`;
  `appsettings.json` sets `uSync:Settings:RootFolder`/`Folders` to it and
  `ImportOnFirstBoot: true`.
- Add the missing Logo SVG under `wwwroot/media/rtoiuk20/`.
- `our-svg` sample resolves the logo version-agnostically (single
  `MediaWithCrops` or first of `IEnumerable<MediaWithCrops>`).
- `launchSettings.json` unchanged (demo still `dotnet run` on 17).

### 2. Playwright harness (`tests/e2e/`)

```
tests/e2e/
  package.json            # @playwright/test pinned
  playwright.config.ts    # projects + webServer per version
  global-setup.ts         # wipes per-version data dirs
  fixtures.ts             # loginToBackoffice(), registerMember(), version info
  specs/
    our-dictionary.spec.ts   our-if.spec.ts        our-form.spec.ts
    our-lang-switcher.spec.ts our-svg.spec.ts      our-fallback.spec.ts
    our-version.spec.ts      our-member-include-exclude.spec.ts
    our-user-include-exclude.spec.ts  our-edit-link.spec.ts
    our-active-class.spec.ts our-link.spec.ts      our-cache.spec.ts
    our-img.spec.ts          our-self-host.spec.ts
```

- **Projects**: `v16`, `v17`, `v18`, `v19`, each with `baseURL`
  `http://localhost:50NN`. `UMBRACO_VERSIONS=17,19` (env) limits which projects
  and web servers start; default all four.
- **webServer** per version: `dotnet run --project <TestSite> -p:UmbracoVersion=N`
  with env: `ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_URLS`,
  `Umbraco__CMS__Global__UseHttps=false`, connection string pointing at a
  per-version SQLite file under `tests/e2e/.data/vN/` (git-ignored). Ready URL =
  home page; generous timeout (install + import).
- **Fresh DB**: global setup deletes `tests/e2e/.data/` before servers start;
  unattended install + uSync first-boot import populate it.
- **Credentials**: backoffice user from the TestSite's unattended settings,
  overridable by env vars.

### 3. What the tests assert (per tag helper)

DOM/attribute assertions only. Every spec also asserts the sample section is
present (guards against "renders nothing").

- **our-edit-link**: anonymous → no edit links. Backoffice login → four links,
  hrefs `/umbraco/section/content/workspace/document/edit/{key}` with the page
  key, `content-key` override honoured, `target="_blank"` and default-styles
  wrapper present.
- **our-user-include-exclude**: anonymous vs logged-in admin visible-line sets
  (as recorded on 2026-11-13).
- **our-member-include-exclude**: anonymous vs a member registered through the
  demo register form during the test.
- **our-img**: alt text (media name fallback + custom), width/height, `srcset`
  breakpoints, `loading`/`fetchpriority`, and that local `src` paths keep their
  leading `/` (regression for the slash bug).
- **our-svg**: both samples render inline `<svg>`, viewBox ensured, no `<our-svg>`
  left in the DOM.
- **our-version**: shows the running Umbraco major for the project.
- **our-if**: element present iff server minute is even — assert consistency
  with the server-rendered time, never a fixed expectation.
- **our-cache**: second request within cache window returns the same timestamp.
- **our-dictionary / our-fallback / our-lang-switcher / our-link /
  our-active-class / form / our-self-host**: expected text, links, classes,
  `ufprt` hidden field, and rewritten self-host URL.
- Cross-cutting: no `<our-*>` elements left unprocessed; page status 200.

### 4. CI (`.github/workflows/e2e.yml`)

- Triggers: `pull_request` and `workflow_dispatch`.
- Matrix over the four versions (parallel jobs, `UMBRACO_VERSIONS=${{ matrix.v }}`),
  `ubuntu-latest`, setup-dotnet 9/10/11 + Node LTS, `npx playwright install
  --with-deps chromium`.
- Upload the HTML report as an artifact on failure.
- While Umbraco 19 is a prerelease, the `v19` matrix job is
  `continue-on-error: true`: it runs and reports red, but does not block the
  PR. When the package moves to Umbraco 19.0.0 stable, remove
  `continue-on-error` in the same change as the version bump.

### 5. Package fix included in this work

`our-img` strips the leading `/` from local file URLs (`AddQueryToUrl` uses
`UriComponents.Path`). Fix so `/samples/x.jpg` stays root-relative; covered by
the `our-img` spec and a unit test. Separate commit.

## Decided by Warren

- Run both locally and in CI from the same command.
- Approach 1 (Playwright Test, TypeScript).
- Demo site on the LTS (Umbraco 17).
- Granular commits, no Claude co-author trailer.
- v19 (release candidate) failures report but do not block PRs until 19 is
  stable.

## Calls made by Claude (for review)

- ProjectReference instead of packing a `.nupkg` for the e2e runs.
- Version-neutral `uSync/site/` folder and `ImportOnFirstBoot`.
- `our-svg` sample made version-agnostic rather than keeping a separate uSync
  data set for 19.
- Chromium only.
- CI as a per-version matrix rather than one job booting four sites.
- Location `tests/e2e/`, ports 5016–5019.

## Left to the builder

- Exact assertions per sample (read each partial).
- Startup timeouts; Playwright/Node versions (pin current stable).
- Whether member registration needs the member type import to succeed on a
  fresh DB (verify; the "Cannot change the Key" uSync warning seen earlier was
  on a pre-populated DB).
