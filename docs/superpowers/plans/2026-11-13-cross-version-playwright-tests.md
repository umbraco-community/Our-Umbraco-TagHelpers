# Cross-version Playwright Tests & LTS Demo Site Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the demo TestSite to Umbraco 17 (LTS) and add a Playwright suite that proves every tag helper sample renders correctly on Umbraco 16, 17, 18 and 19.0.0-rc1, locally and in GitHub Actions.

**Architecture:** The TestSite gets an `UmbracoVersion` MSBuild switch (default 17). A Node start script publishes the site per version into an isolated folder (`tests/e2e/.data/vN/app`) and runs it on its own port with a fresh SQLite DB populated by uSync on first boot. Playwright's `webServer` array runs that script once per selected version; one Playwright project per version runs every spec.

**Tech Stack:** .NET 9/10/11 SDKs, Umbraco 16.5.1 / 17.8.1 / 18.3.1 / 19.0.0-rc1, uSync, Node LTS, `@playwright/test` (Chromium), NUnit + Moq (unit test for the img fix), GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-11-13-cross-version-playwright-tests-design.md`

## Global Constraints

- Branch `v2/dev`. One commit per task step marked Commit. **No `Co-Authored-By` trailer, no Claude attribution** in any commit.
- Version matrix (exact): 16 → `net9.0`, Umbraco.Cms `16.5.1`, uSync `16.1.0`, port `5016`; 17 → `net10.0`, `17.8.1`, `17.6.2`, `5017`; 18 → `net10.0`, `18.3.1`, `18.3.1`, `5018`; 19 → `net11.0`, `19.0.0-rc1`, `19.0.0-rc1`, `5019`.
- Default `UmbracoVersion` is `17`; `dotnet run` in the TestSite must give a working 17 demo on a fresh clone (DB is git-ignored).
- TestSite keeps a **ProjectReference** to `Our.Umbraco.TagHelpers` (no local NuGet feed).
- Tests run over HTTP with env `Umbraco__CMS__Global__UseHttps=false` (never committed to appsettings).
- Assertions are DOM/attribute/text only — no screenshot comparison. Screenshots/traces only on failure.
- `UMBRACO_VERSIONS` env (comma list, e.g. `17,19`) selects versions; default `16,17,18,19`.
- Generated output (`tests/e2e/.data/`, `node_modules/`, `playwright-report/`, `test-results/`) is git-ignored.
- CI: v19 job `continue-on-error: true` while 19 is a prerelease.
- Never modify files outside the repo; never touch `Our.Umbraco.TagHelpers.TestSite/umbraco/Data/`.

## Review Focus

1. **A sample silently renders nothing** (tag helper suppressed output or threw) → spec must fail. Pinned in Task 4 (`smoke.spec.ts`: every sample `<h2>` present, zero unprocessed `our-*` elements) and per-sample non-empty assertions in Tasks 5–7.
2. **Required .NET runtime missing locally** (this machine has no .NET 9) → the v16 server must fail fast with a clear message, not hang until the webServer timeout. Pinned in Task 4 Step "runtime guard".
3. **Stale data from a previous run** (DB, uSync first-boot flag, downloaded self-host assets) → each run starts from a wiped `tests/e2e/.data/vN/`. Pinned in Task 4 (run twice; second run still imports).
4. **Time-dependent samples** (`our-if` even/odd minute, `our-cache` timestamps) → never assert fixed values. Pinned in Task 5 (`our-if`: exactly one of the two lines; `our-cache`: same timestamp on reload).
5. **Login state leaking between versions** (localhost cookies ignore port) → each test uses Playwright's default fresh context; anonymous assertions run in a context that never logged in. Pinned in Task 7 (anonymous test asserts no edit links even after another test logged in).

---

### Task 1: Keep root-relative `src` in `our-img` (package fix)

**Files:**
- Modify: `Our.Umbraco.TagHelpers/ImgTagHelper.cs` (`AddQueryToUrl`, ~line 454)
- Test: `Our.Umbraco.TagHelpers.Tests/ImgTagHelperTests.cs` (create)

**Interfaces:**
- Produces: behaviour — for `src="/samples/cute-unicorn.jpg"`, every emitted URL (`src`, `<source srcset>`) starts with `/samples/cute-unicorn.jpg?`.

- [ ] **Step 1: Write failing tests** in `ImgTagHelperTests`:
  - `FileSource_RootRelative_KeepsLeadingSlash`: `new ImgTagHelper(Options.Create(new OurUmbracoTagHelpersConfiguration()))` with `FileSource = "/samples/cute-unicorn.jpg"`, `ImgWidth = 340`, `ImgHeight = 170`, `ViewContext` whose `HttpContext` is a `DefaultHttpContext` with `Request.Scheme="http"`, `Host="localhost:5017"`, `Path="/en/"`. After `Process`, assert `output.Attributes["src"].Value.ToString()` starts with `"/samples/cute-unicorn.jpg?width=340"`.
  - `FileSource_AbsoluteUrl_Unchanged`: `FileSource = "https://example.com/a.jpg"`, same sizes → `src` starts with `"https://example.com/a.jpg?width=340"`.
- [ ] **Step 2: Run** `dotnet test Our.Umbraco.TagHelpers.Tests -f net10.0 --filter ImgTagHelperTests` → first test FAILS (`src` is `samples/cute-unicorn.jpg?...`).
- [ ] **Step 3: Fix `AddQueryToUrl(string url, string key, string value) : string`** — for relative URLs build the base from `uri.AbsolutePath` (keeps the leading `/`) instead of `GetComponents(UriComponents.Path, …)`. Absolute URLs unchanged.
- [ ] **Step 4: Run** `dotnet test Our.Umbraco.TagHelpers.Tests -f net10.0` and `-f net11.0` → all pass (42 tests).
- [ ] **Step 5: Commit** `Keep root-relative paths in our-img file URLs`

---

### Task 2: Version-switchable TestSite, default Umbraco 17 LTS

**Files:**
- Modify: `Our.Umbraco.TagHelpers.TestSite/Our.Umbraco.TagHelpers.TestSite.csproj` (replaces the uncommitted 19-rc1 edits)
- Create: `Our.Umbraco.TagHelpers.TestSite/Directory.Build.props`
- Modify: `Our.Umbraco.TagHelpers.TestSite/Views/Partials/blocklist/default.cshtml:8`

**Interfaces:**
- Produces: `dotnet build|publish <TestSite> -p:UmbracoVersion=<16|17|18|19>`; outputs under `bin/u<N>/`, `obj/u<N>/`.

- [ ] **Step 1: Verify the current failure**: `dotnet build Our.Umbraco.TagHelpers.TestSite -p:UmbracoVersion=16` builds 19-rc1 (property ignored) — note it.
- [ ] **Step 2: Implement** csproj: `<UmbracoVersion Condition="'$(UmbracoVersion)' == ''">17</UmbracoVersion>`; per-version `PropertyGroup` setting `TargetFramework`, `UmbracoCmsVersion`, `USyncVersion` from the matrix; `PackageReference`s use those properties (keep `Umbraco.Cms`, `Umbraco.Cms.Core`, `Umbraco.Cms.Web.Common`, `uSync`); fail the build with `<Error>` for any other value. Set `RazorCompileOnBuild` and `RazorCompileOnPublish` to `true`. Keep `LangVersion` default and the ICU items.
- [ ] **Step 3: Implement** `Directory.Build.props` (TestSite folder only): same default for `UmbracoVersion`; `BaseIntermediateOutputPath=obj\u$(UmbracoVersion)\`, `BaseOutputPath=bin\u$(UmbracoVersion)\`. Confirm the library and test projects are unaffected (they are outside this folder).
- [ ] **Step 4: Fix blocklist partial**: `block?.ContentUdi == null` → `block?.Content == null`.
- [ ] **Step 5: Verify** `dotnet build Our.Umbraco.TagHelpers.TestSite -p:UmbracoVersion=N` succeeds for 16, 17, 18, 19 and `obj/u16 … obj/u19` each contain their own `project.assets.json`; plain `dotnet build Our.Umbraco.TagHelpers.slnx` succeeds (17).
- [ ] **Step 6: Commit** `Make the TestSite switchable across Umbraco 16-19, default to 17 LTS`

---

### Task 3: Demo content works on a fresh database for every version

**Files:**
- Move: `Our.Umbraco.TagHelpers.TestSite/uSync/v16/` → `uSync/site/` (`git mv`)
- Modify: `Our.Umbraco.TagHelpers.TestSite/appsettings.json` (add `uSync:Settings`)
- Modify: `Our.Umbraco.TagHelpers.TestSite/Our.Umbraco.TagHelpers.TestSite.csproj` (publish `uSync/**` if not already in publish output)
- Create: `Our.Umbraco.TagHelpers.TestSite/wwwroot/media/rtoiuk20/face-savoring-food.svg` (copy of `wwwroot/samples/face-savoring-food.svg`)
- Modify: `Views/Partials/taghelper-samples/our-svg.cshtml`
- Modify: `Views/Partials/taghelper-samples/our-member-include-exclude.cshtml`

**Interfaces:**
- Produces: on a fresh DB, first boot imports all content (8 docs, 3 languages/domains, 2 media, dictionary items); `our-svg` renders 2 SVGs on every version; logged-out visitors see the register form under the member sample.

- [ ] **Step 1: Verify the failure**: with a temp copy of the site's DB folder removed (run from `bin/u17` via `dotnet run -p:UmbracoVersion=17 --no-launch-profile` with `ASPNETCORE_URLS=http://localhost:5117`, `ConnectionStrings__umbracoDbDSN` pointing at a temp SQLite file), home shows "No published content".
- [ ] **Step 2: Implement** `git mv uSync/v16 uSync/site`; `appsettings.json` → `"uSync": { "Settings": { "RootFolder": "uSync/site/", "Folders": [ "uSync/Root/", "uSync/site/" ], "ImportOnFirstBoot": true } }`; ensure `uSync/**` is copied on publish.
- [ ] **Step 3: Implement** add the Logo SVG file; in `our-svg.cshtml` resolve the logo as `MediaWithCrops? logo = Model.Value("logo") switch { MediaWithCrops m => m, IEnumerable<MediaWithCrops> l => l.FirstOrDefault(), _ => null };` and pass `logo` to `media-item`.
- [ ] **Step 4: Implement** member sample: render `<partial name="membership/register" />` only when no member is logged in (keep login + login-status partials).
- [ ] **Step 5: Verify** fresh-DB boot on 17 (Step 1 command) and on 19 (`-p:UmbracoVersion=19`, port 5119): `/`, `/en/`, `/es/`, `/da/`, `/en/page-a/` return 200; home has exactly 2 `<svg`; log has `uSync` first-boot import and no `ERR`. Delete the temp DBs.
- [ ] **Step 6: Commit** in two commits: `Move uSync data to a version-neutral folder and import on first boot` (Step 2) and `Make demo samples work on a fresh install across versions` (Steps 3–4).

---

### Task 4: Playwright harness, start script and smoke spec

**Files:**
- Create: `tests/e2e/package.json`, `tests/e2e/package-lock.json`, `tests/e2e/tsconfig.json`
- Create: `tests/e2e/scripts/start-site.mjs`
- Create: `tests/e2e/playwright.config.ts`
- Create: `tests/e2e/fixtures.ts`
- Create: `tests/e2e/specs/smoke.spec.ts`
- Modify: root `.gitignore` (or create `tests/e2e/.gitignore`)

**Interfaces:**
- Produces:
  - `node scripts/start-site.mjs <16|17|18|19>`: checks `dotnet --list-runtimes` has `Microsoft.AspNetCore.App <major>.` for the version's TFM, else prints `Umbraco <N> needs the .NET <M> runtime` and exits 1; wipes and recreates `tests/e2e/.data/v<N>/`; `dotnet publish <TestSite> -c Release -p:UmbracoVersion=<N> -o .data/v<N>/app`; spawns `dotnet Our.Umbraco.TagHelpers.TestSite.dll` with `cwd=.data/v<N>/app` and env `ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_URLS=http://localhost:50<N>`, `Umbraco__CMS__Global__UseHttps=false`; forwards SIGINT/SIGTERM.
  - `playwright.config.ts`: `versions = (process.env.UMBRACO_VERSIONS ?? '16,17,18,19')` parsed to numbers; `projects: versions.map(v => ({ name: \`v${v}\`, use: { baseURL: \`http://localhost:50${v}\` }, metadata: { umbracoMajor: v } }))`; `webServer: versions.map(v => ({ command: \`node scripts/start-site.mjs ${v}\`, url: \`http://localhost:50${v}/\`, timeout: 600_000, reuseExistingServer: !process.env.CI }))`; `use: { trace: 'retain-on-failure', screenshot: 'only-on-failure' }`; `reporter: [['list'], ['html', { open: 'never' }]]`; `retries: process.env.CI ? 1 : 0`.
  - `fixtures.ts`: `test` extended with `umbracoMajor: number` (from project metadata); `loginToBackoffice(page: Page): Promise<void>` (goes to `/umbraco`, fills email/password from `E2E_BACKOFFICE_EMAIL`/`E2E_BACKOFFICE_PASSWORD` defaulting to the TestSite unattended values, waits for URL `/umbraco/section/content`); `registerMember(page: Page): Promise<{ name: string; email: string }>` (unique email per call, submits the register form on `/en/`, waits for the login-status "Welcome back"); `section(page: Page, heading: string): Locator` (everything from the `<h2>` with that text up to the next `<hr>`).
  - `export { expect }` from fixtures.

- [ ] **Step 1: Write `smoke.spec.ts`**:
  - `home renders every sample`: `/en/` status 200; each of these `<h2>` texts visible: `our-dictionary`, `our-if`, `Replacement for BeginUmbracoForm`, `our-lang-switcher`, `our-svg`, `our-fallback`, `our-version`, `our-member-include and our-member-exclude`, `our-user-include and our-user-exclude`, `our-edit-link`, `our-active-class`, `our-link`, `our-cache`, `our-img`, `our-self-host`.
  - `no unprocessed our-* tags`: `page.evaluate` count of elements whose tagName starts with `OUR-` is `0`, and of elements with any attribute starting `our-` is `0`.
  - `culture pages render`: `/`, `/es/`, `/da/`, `/en/page-a/`, `/en/page-a/page-ab/`, `/en/page-b/page-ba/`, `/en/page-c/page-ca/` all status 200.
- [ ] **Step 2: Run** `cd tests/e2e && npm ci && npx playwright install chromium && UMBRACO_VERSIONS=17 npx playwright test specs/smoke.spec.ts` before writing the start script/config → fails (no config/server).
- [ ] **Step 3: Implement** package files (pin current stable `@playwright/test` and `typescript`; scripts `"test": "playwright test"`), `start-site.mjs`, `playwright.config.ts`, `fixtures.ts` per Interfaces; ignore rules.
- [ ] **Step 4: Run** `UMBRACO_VERSIONS=17 npx playwright test specs/smoke.spec.ts` → 3 passed. Run again → still passes (fresh data each run; confirms Review Focus 3).
- [ ] **Step 5: Runtime guard**: on this machine (no .NET 9 runtime) `UMBRACO_VERSIONS=16 npx playwright test specs/smoke.spec.ts` fails within 60 s with `Umbraco 16 needs the .NET 9 runtime`.
- [ ] **Step 6: Run** `UMBRACO_VERSIONS=17,18,19 npx playwright test specs/smoke.spec.ts` → 9 passed (three sites boot in parallel without file/port conflicts).
- [ ] **Step 7: Commit** `Add Playwright harness that boots the TestSite per Umbraco version`

---

### Task 5: Specs for content and markup tag helpers

**Files:**
- Create in `tests/e2e/specs/`: `our-dictionary.spec.ts`, `our-fallback.spec.ts`, `our-lang-switcher.spec.ts`, `our-link.spec.ts`, `our-active-class.spec.ts`, `our-if.spec.ts`, `our-cache.spec.ts`, `our-form.spec.ts`, `our-self-host.spec.ts`, `our-version.spec.ts`

**Interfaces:**
- Consumes: `test`, `expect`, `section(page, heading)`, `umbracoMajor` from `fixtures.ts`.

Expected values come from the sample partials under `Our.Umbraco.TagHelpers.TestSite/Views/Partials/taghelper-samples/` and the uSync dictionary/content data; where a value depends on data, read it from `uSync/site/` and hard-code it in the spec. Each spec runs on `/en/` unless stated.

- [ ] **Step 1: Write the specs** (one `test` per bullet):
  - dictionary: `home` key renders the EN translation (not `My Header`); `test` key with `fallback-lang="da"` renders the DA value; `nonExistantKey` renders `I will render as no key exists`. Also on `/da/`, the `home` key renders the DA translation.
  - fallback: first `<h3>` shows the DA `Header` value (not the placeholder text); second shows `I do NOT have a da-DK culture variant of this property`.
  - lang-switcher: templated block has 3 `a.lang-switcher__link` with hrefs `/en`, `/es`, `/da` (any order) and exactly one with class `selected` whose `hreflang` is `en-US`; on `/da/` the selected one is `da`; default (untemplated) switcher renders a `<ul>` with 3 links.
  - link: `Hi There` is inside an `<a>` with the CTA link's href/target from content; empty-link fallbacks render `div > span` and a bare `span` with the given text.
  - active-class: on `/en/page-a/` the `Page A` anchor has class `nav-link--active` and the `li` for Page A has `selected`; on `/en/` none do. If the sample's documented limitation ("does not work with domain cultures") means this fails on all versions, record the actual 17 behaviour as the expectation and add a `test.fixme` naming the limitation.
  - if: within the section, exactly one of `This will only render during even minutes` / `odd minutes` is present.
  - cache: reload `/en/` within 5 s; the `Every 120 seconds` line's timestamp is identical across both loads.
  - form (`Replacement for BeginUmbracoForm`): `form#testContactForm` has a hidden `input[name="ufprt"]` with non-empty value; no `our-controller`/`our-action` attributes remain.
  - self-host: each `<script>` in the section has a `src` starting with `/assets/` (folder-attribute one under `/assets/libraries/`, extensionless one ending `alpinejs.js`); requesting the first `src` returns 200.
  - version: `Umbraco.Core:` line starts with `${umbracoMajor}.`; `Our.Umbraco.Taghelpers:` line is non-empty; `Non existent:` line is empty after the label.
- [ ] **Step 2: Run** `UMBRACO_VERSIONS=17 npx playwright test specs/our-*.spec.ts` → all pass; fix expectations only against the sample source/uSync data, never by loosening to "non-empty".
- [ ] **Step 3: Run** `UMBRACO_VERSIONS=17,18,19 npx playwright test` → all pass.
- [ ] **Step 4: Commit** `Add cross-version specs for content and markup tag helpers`

---

### Task 6: Specs for media tag helpers

**Files:**
- Create: `tests/e2e/specs/our-img.spec.ts`, `tests/e2e/specs/our-svg.spec.ts`

**Interfaces:**
- Consumes: fixtures from Task 4; Task 1's root-relative URL behaviour; Task 3's logo file and version-agnostic sample.

- [ ] **Step 1: Write the specs**:
  - img: media image without `alt` gets `alt="ashim-d-silva-WeYamle9fDM-unsplash.jpg"` (media name); custom alt sample has `alt="Custom Alt Text"`, `width="200"`, `height="133.33333333333331"`; `<picture>` sample has `<source>` entries with `media="(min-width: 768px)"` width 600 and `(min-width: 576px)` width 400; lazy images have `loading="lazy"`, above-the-fold one has `loading="eager"` + `fetchpriority="high"`; every `img[src*="cute-unicorn"]` and `source[srcset*="cute-unicorn"]` URL starts with `/samples/`; every image URL in the section returns 200 (request it).
  - svg: section contains exactly 2 inline `<svg>` elements, each with a `viewbox` attribute, and no `<img>`/`<our-svg>`.
- [ ] **Step 2: Run** `UMBRACO_VERSIONS=17,18,19 npx playwright test specs/our-img.spec.ts specs/our-svg.spec.ts` → all pass (the slash assertion would fail without Task 1).
- [ ] **Step 3: Commit** `Add cross-version specs for img and svg tag helpers`

---

### Task 7: Specs for authentication-aware tag helpers

**Files:**
- Create: `tests/e2e/specs/our-edit-link.spec.ts`, `tests/e2e/specs/our-user-include-exclude.spec.ts`, `tests/e2e/specs/our-member-include-exclude.spec.ts`

**Interfaces:**
- Consumes: `loginToBackoffice`, `registerMember`, `section` from `fixtures.ts`.

- [ ] **Step 1: Write the specs**:
  - edit-link anonymous: section has 0 `a[href*="/umbraco/section/content/workspace/document/edit/"]`.
  - edit-link backoffice user: after `loginToBackoffice`, `/en/` has 4 such links; three point at `b0c59e4c-158c-4fd4-9beb-ebe907693f1c`, one at `b0c59e4c-158c-4fd4-9beb-ebe907693e4a`; the `target="_blank"` sample keeps its target; the `use-default-styles` one is wrapped in a `div` with `position:fixed` style.
  - edit-link isolation: a test that logs in, then a second test in the same file (fresh context) asserts 0 links (Review Focus 5).
  - user include/exclude anonymous: visible lines exactly `Only ✅ Anonymous users will see this.`, `Everyone except ❌ Editor users can see this.`, `Everyone except ❌ Admin or ❌ Editor users can see this.`, `Everyone except ❌ logged in users will see this.`
  - user include/exclude admin: after login, visible lines exactly `Only users in the ✅ Admin group will see this.`, `Only users in the ✅ Admin or ✅ Editor user group will see this.`, `Only ✅ logged in users will see this.`, `Everyone except ❌ Editor users can see this.`, `Everyone except ❌ Admin or ❌ Editor users can see this.`, `Everyone except ❌ Anonymous users will see this.`
  - member include/exclude anonymous vs registered member: the `?`/`*` include and exclude lines flip as for users; `Staff` and `Staff,Admins` include lines are hidden in both states (no member groups exist); `Everyone except ❌ Staff members can see this.` visible in both.
- [ ] **Step 2: Run** `UMBRACO_VERSIONS=17,18,19 npx playwright test specs/our-edit-link.spec.ts specs/our-user-include-exclude.spec.ts specs/our-member-include-exclude.spec.ts` → all pass. If member registration fails on a fresh DB (member type import), fix the uSync member type data, not the test.
- [ ] **Step 3: Commit** `Add cross-version specs for edit link, user and member tag helpers`

---

### Task 8: CI workflow and docs

**Files:**
- Create: `.github/workflows/e2e.yml`
- Modify: `README.md` (short "Cross-version tests" section)

**Interfaces:**
- Consumes: `tests/e2e` npm scripts and `UMBRACO_VERSIONS`.

- [ ] **Step 1: Implement** `e2e.yml`: `on: [pull_request, workflow_dispatch]`; job `e2e` on `ubuntu-latest` with `strategy.fail-fast: false`, `matrix.umbraco: [16, 17, 18, 19]`; `continue-on-error: ${{ matrix.umbraco == 19 }}`; steps: checkout, `actions/setup-dotnet@v4` with `9.0.x`/`10.0.x`/`11.0.x`, `actions/setup-node@v4` with `lts/*` + npm cache on `tests/e2e/package-lock.json`, `npm ci`, `npx playwright install --with-deps chromium`, `npx playwright test` with `env: UMBRACO_VERSIONS: ${{ matrix.umbraco }}`, and `actions/upload-artifact@v4` of `tests/e2e/playwright-report` named `playwright-report-v${{ matrix.umbraco }}` `if: failure()`. Add a comment next to `continue-on-error` to remove it when moving to Umbraco 19.0.0 stable.
- [ ] **Step 2: Verify** the YAML parses (`npx --yes js-yaml .github/workflows/e2e.yml`, or `actionlint` if installed).
- [ ] **Step 3: Implement** README section: prerequisites (.NET 9/10/11 SDKs or runtimes for the versions you run, Node LTS), `cd tests/e2e && npm ci && npx playwright install chromium && npx playwright test`, `UMBRACO_VERSIONS=17,19` example, `npx playwright show-report`, and that the demo site runs Umbraco 17 by default with `-p:UmbracoVersion=N` to switch.
- [ ] **Step 4: Full local run** `UMBRACO_VERSIONS=17,18,19 npx playwright test` → all pass. v16 needs the .NET 9 runtime, which this machine lacks: **ask Warren before installing anything outside the repo**; otherwise report v16 as covered by CI only.
- [ ] **Step 5: Commit** `Run cross-version Playwright tests in GitHub Actions` (workflow) and `Document cross-version tests and the LTS demo site` (README).
