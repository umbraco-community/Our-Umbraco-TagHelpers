# Tag Helper Option Coverage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a demo sample and a cross-version Playwright scenario (Umbraco 16–19) for every tag helper option the suite doesn't cover yet.

**Architecture:** The existing setup project (`specs/backoffice.setup.ts`) also seeds, through the Management API, the identities the new tests need: an Editor user, a user without Content access, and a `Staff` member. It saves a session for each. Specs then use those sessions through fixtures. Content publishing and preview go through the same API helper. Demo samples are added to the existing partials in `Our.Umbraco.TagHelpers.TestSite/Views/Partials/taghelper-samples/`.

**Tech Stack:** Playwright 1.64 + TypeScript in `tests/e2e`, Umbraco 16.5.1 / 17.8.1 / 18.3.1 / 19.0.0-rc1 demo site (Razor, uSync).

**Spec:** `docs/superpowers/specs/2026-11-13-tag-helper-option-coverage-design.md`

## Global Constraints

- Branch `v2/feature/multi-version-support`. One commit per task (or per step where a task says so). **No `Co-Authored-By` trailer.** Nothing is pushed.
- Same four sites, one per version, and the same `npm test` entry point. No extra sites.
- Users, user groups, members and member groups are created by the tests, not committed. Committed demo content (uSync) is fine.
- Specs may run in parallel. Only `our-cache.spec.ts` publishes content or enters preview, and it runs `test.describe.configure({ mode: 'serial' })`.
- Out of scope: site-wide `our-img` appsettings; `our-active-class` active marking (issue #97, stays `test.fixme`).
- **Stop any running demo sites before each test run.** `reuseExistingServer` reuses a running site locally, and views are compiled at publish, so a running site serves stale views. Check with `lsof -iTCP:5016-5019 -sTCP:LISTEN`.
- Iterate on one version: `UMBRACO_VERSIONS=17 npm test -- specs/<file>`. Each task ends with the same spec on all four versions: `npm test -- specs/<file>`.

## Facts established by probing (2026-11-13, all four versions)

- **Management API auth:** On 17, 18 and 19 the backoffice keeps the token in httpOnly cookies (`umbAccessToken`). A request carrying the saved session cookies plus the literal header `Authorization: Bearer [redacted]` is authorised. On 16 the token is in `localStorage['umb:userAuthTokenResponse']` (JSON, `access_token`), which Playwright's `storageState` keeps under `origins[].localStorage`. Send `Authorization: Bearer <access_token>`.
- **Endpoints** (base `/umbraco/management/api/v1`): `GET /user-group?take=50` (`items[].alias`, `id`); `POST /user-group`; `POST /user` (`{email,userName,name,userGroupIds:[{id}],kind:'Default'}`); `POST /user/{id}/change-password` (`{newPassword}`); `POST /member-group` (`{name}`; 400 "Duplicate name" if it exists); `GET /tree/member-group/root?take=50`; `GET /tree/member-type/root?take=10` (one type, `Member`); `POST /member` (`{email,username,password,memberType:{id},groups:[groupId],isApproved:true,values:[],variants:[{name,culture:null,segment:null}]}`); `PUT /document/{id}/publish` (`{publishSchedules:[{culture}]}`). New IDs come from the `Location` header (last path segment).
- **User group payload:** Umbraco 19 requires `hasAccessToInvariantForVariant`, `elementRootAccess` and `documentBlueprintRootAccess`. 16 to 18 accept them as extras. Send `{name, alias, sections:['Umb.Section.Media'], languages:[], hasAccessToAllLanguages:true, documentRootAccess:false, mediaRootAccess:true, hasAccessToInvariantForVariant:false, elementRootAccess:false, documentBlueprintRootAccess:false, fallbackPermissions:[], permissions:[]}`.
- **Preview:** On 16 and 17, `POST /preview` sets the `UMB_PREVIEW` cookie (deprecated in 17). On 18 and 19 that returns 404; `GET /document/{key}/preview-url?providerAlias=umbDocumentUrlProvider&culture=en-US` sets `UMB_PREVIEW` instead. With the cookie, `/en/` renders in preview and `our-cache` doesn't cache.
- **Debug:** `/en/?umbdebug=true` skips `our-cache` on all four versions (`Hosting:Debug` is true in Development).
- **Content:** Home `b0c59e4c-158c-4fd4-9beb-ebe907693f1c` varies by culture (`en-US`, `es`, `da`). Page A `c455b06a-7fcc-4959-bacf-dc5e1dbc0c21` is invariant (publish with `[{culture:null}]`), URL `/en/page-a/`. Every page has a `header` value. Publishing returns 200 on all four versions.
- **`our-cache` timestamps** contain U+202F before AM/PM, so match with `/^(.*) - <label>/m`, never `\S+ \S+`.

## Review Focus

1. **Running a second time against a reused local site** (same DB): seeding must not fail on an existing `Staff` group or `e2eNoContent` user group. Get-or-create those; make user and member emails unique per run. (Task 1 runs setup twice.)
2. **A publish from one cache test leaking into another**: `our-cache.spec.ts` is serial, and every timestamp comparison waits more than 1 s first. (Task 4.)
3. **The `our-svg` cache test editing a file on disk** must restore it even when the test fails (`try/finally`), or later runs on a reused site see the changed file. (Task 6.)
4. **The `style` attribute on `our-img`**: in `ImgTagHelper` the bound `style` is only re-added when `ApplyAspectRatio` is on, so it is probably dropped by default. The test asserts it is kept. If it fails, that's a library bug, handled like #97 (Task 7, Step 4).
5. **Existing specs that count elements** (`our-svg` `svg` count 2, `our-img` `nth(0)`) must keep passing after samples are added. Wrap new SVG samples in their own `data-demo` elements; append new `our-img` rows at the end of the table.

---

### Task 1: Seed identities through the Management API

**Files:**
- Create: `tests/e2e/umbraco-api.ts`
- Modify: `tests/e2e/fixtures.ts`, `tests/e2e/specs/backoffice.setup.ts`, `tests/e2e/specs/our-active-class.spec.ts` (fixme comment only)
- Modify: `Our.Umbraco.TagHelpers.TestSite/Views/Partials/taghelper-samples/our-user-include-exclude.cshtml`, `.../our-member-include-exclude.cshtml` (sample fixes)
- Test: `tests/e2e/specs/our-user-include-exclude.spec.ts`

**Interfaces:**
- Produces in `fixtures.ts`:
  - `type Identity = 'backoffice' | 'editor' | 'no-content-user' | 'staff-member'`
  - `authFile(umbracoMajor: number, identity: Identity): string` → `.data/v<N>/<identity>-auth.json`. `backofficeAuthFile(n)` stays as `authFile(n, 'backoffice')`, so the file name doesn't change.
  - Fixtures `backofficePage`, `editorPage`, `noContentUserPage`, `staffMemberPage: Page`, each a new context with that storage state, closed after use.
  - `loginToBackoffice(page, credentials?: { email: string; password: string })`. Defaults to today's env/admin values. Waits for `**/umbraco/section/**`, not `.../content**`, because the no-content user lands in Media.
  - `loginMember(page, credentials: { email: string; password: string })`: fills the demo `membership/login` form on `/en/` and expects `Welcome back`.
- Produces in `umbraco-api.ts`:
  - `authorizationHeader(storageStateFile: string): string`: `Bearer <access_token>` from `umb:userAuthTokenResponse` if present, else `Bearer [redacted]`.
  - `class ManagementApi` with `static async create(baseURL: string, storageStateFile: string)` (wraps `request.newContext({ baseURL, storageState, extraHTTPHeaders })`) and:
    `userGroupId(alias: string): Promise<string>`, `ensureUserGroup(name: string, alias: string, sections: string[]): Promise<string>`, `createUser(u: { name: string; email: string; password: string; groupIds: string[] }): Promise<string>`, `ensureMemberGroup(name: string): Promise<string>`, `createMember(m: { name: string; email: string; password: string; groupIds: string[] }): Promise<string>`, `publish(documentKey: string, cultures: (string | null)[]): Promise<void>`, `enterPreview(documentKey: string): Promise<void>` (tries `POST /preview`, then `preview-url` on 404), `dispose(): Promise<void>`. Every method throws with the status and body on an unexpected status.
  - Constants `homeKey`, `pageAKey` (values in Facts).

- [ ] **Step 1: Write the failing tests** in `our-user-include-exclude.spec.ts`. Keep the anonymous test. Update the admin test for the sample fix (the admin no longer sees "Everyone except ❌ Admin or ❌ Editor users can see this."). Add:

```ts
test('editor sees the editor and logged-in variants', async ({ editorPage: page }) => {
  await page.goto('/en/');
  await expect(visibleLines(page)).toHaveText([
    'Only users in the ✅ Admin or ✅ Editor user group will see this.',
    'Only ✅ logged in users will see this.',
    'Everyone except ❌ Anonymous users will see this.',
  ]);
});

test('user outside admin and editor groups sees only the logged-in variants', async ({ noContentUserPage: page }) => {
  await page.goto('/en/');
  await expect(visibleLines(page)).toHaveText([
    'Only ✅ logged in users will see this.',
    'Everyone except ❌ Editor users can see this.',
    'Everyone except ❌ Admin or ❌ Editor users can see this.',
    'Everyone except ❌ Anonymous users will see this.',
  ]);
});
```

- [ ] **Step 2: Run to verify it fails.** `UMBRACO_VERSIONS=17 npm test -- specs/our-user-include-exclude.spec.ts`. Expected: FAIL (TypeScript error or missing fixture `editorPage`).

- [ ] **Step 3: Implement.**
  - Write `umbraco-api.ts` and the fixture changes above.
  - In `backoffice.setup.ts`, after saving the admin state: create a `ManagementApi` from it and seed. The Editor user goes in the `editor` group. The `e2eNoContent` group is named "E2E No Content", with `['Umb.Section.Media']`. The `Staff` member group gets one member in it. Use emails `e2e-<identity>-<Date.now()>@example.com` and one strong password constant. Log each identity in, in its own browser context (backoffice users with `loginToBackoffice(page, creds)`, the member with `loginMember`), and save `authFile(major, identity)`.
  - Sample fixes: in the user sample, the second exclude line becomes `our-user-exclude="admin,editor"`. In both samples, the special-group list becomes `? - All anonymous users` / `* - All authenticated users`.
  - In `our-active-class.spec.ts`, replace the fixme comment's last sentence with `Tracked in https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/97 — remove fixme when fixed.`
  - If the first login of a user created through the API lands on a password-change or welcome screen on any version, record that as a Ruling and handle it in `loginToBackoffice`.

- [ ] **Step 4: Run on 17, then all versions.** `UMBRACO_VERSIONS=17 npm test -- specs/our-user-include-exclude.spec.ts`, then `npm test -- specs/our-user-include-exclude.spec.ts`. Expected: all pass on 16–19. Then, **with the sites still running**, run the same command again. Expected: pass (seeding is idempotent, Review Focus 1). Stop the sites afterwards.

- [ ] **Step 5: Run the typecheck.** `npm run typecheck`. Expected: no errors.

- [ ] **Step 6: Commit.** `git commit -m "Seed an Editor user, a user without content access and a Staff member for the e2e tests"`. Include the user/member sample fixes and the #97 link.

### Task 2: `our-edit-link` for a user without content access

**Files:** Test: `tests/e2e/specs/our-edit-link.spec.ts`

**Interfaces:** Consumes the `noContentUserPage` and `editorPage` fixtures (Task 1).

- [ ] **Step 1: Write the tests.** Add `test('hidden for a backoffice user without access to content', …)`: `noContentUserPage` on `/en/` expects `section(page,'our-edit-link').locator('a')` to have count 0. Add `test('shown to an editor', …)`: `editorPage` expects count 4.
- [ ] **Step 2: Run.** `UMBRACO_VERSIONS=17 npm test -- specs/our-edit-link.spec.ts`. Expected: PASS. No production change is expected, because the behaviour already exists (unit tested in `EditLinkTagHelperTests`). To prove the test can fail, temporarily switch the no-content fixture to `editorPage`, watch it fail with count 4, then revert.
- [ ] **Step 3: Run all versions.** `npm test -- specs/our-edit-link.spec.ts`. Expected: PASS.
- [ ] **Step 4: Commit.** `git commit -m "Test our-edit-link for users with and without content access"`.

### Task 3: `our-member` with a Staff member

**Files:** Test: `tests/e2e/specs/our-member-include-exclude.spec.ts`

**Interfaces:** Consumes the `staffMemberPage` fixture.

- [ ] **Step 1: Write the test.**

```ts
test('Staff member sees the Staff and logged-in variants', async ({ staffMemberPage: page }) => {
  await page.goto('/en/');
  await expect(sampleLines(page)).toHaveText([
    'Only members of ✅ Staff Member Group will see this.',
    'Only members of ✅ Staff OR ✅ Admins member group will see this.',
    'Only ✅ logged in members will see this.',
    'Everyone except ❌ Anonymous users will see this.',
  ]);
});
```

Update the comment on the existing logged-in test: it uses a member with no groups.
- [ ] **Step 2: Run.** `UMBRACO_VERSIONS=17 npm test -- specs/our-member-include-exclude.spec.ts`. Expected: PASS. If it fails, check the member's groups in the backoffice before changing anything else.
- [ ] **Step 3: Run all versions.** Expected: PASS.
- [ ] **Step 4: Commit.** `git commit -m "Test our-member include/exclude for a member of the Staff group"`.

### Task 4: `our-cache` publish, opt-out, vary-by, debug and preview

**Files:**
- Modify: `Our.Umbraco.TagHelpers.TestSite/Views/Partials/taghelper-samples/our-cache.cshtml`. Change only the opt-out block's text, to `@DateTime.Now - Not cleared by publishing other pages`, so it can be read on its own.
- Test: `tests/e2e/specs/our-cache.spec.ts`

**Interfaces:** Consumes `ManagementApi` (`publish`, `enterPreview`), `homeKey`, `pageAKey`, `authFile`.

- [ ] **Step 1: Write the failing tests.** Add `test.describe.configure({ mode: 'serial' })`. Extend `timestamps()` with `optOut: /^(.*) - Not cleared by publishing other pages/m`. Add:
  - `publishing a page clears the cached content`: read `cached`, wait 1.5 s, `publish(pageAKey, [null])`, reload, expect `cached` to differ.
  - `update-cache-on-publish="false" keeps its content when another page is published`: read `optOut`, wait 1.5 s, publish Page A, reload, expect `optOut` unchanged.
  - `vary-by renders new content when its value changes`: read `optOut`, wait 1.5 s, `publish(homeKey, ['en-US','es','da'])` (changes `Model.UpdateDate`), reload, expect `optOut` to differ.
  - `is skipped in debug mode`: two loads of `/en/?umbdebug=true` 1.5 s apart, expect `cached` to differ.
  - `is skipped in preview`: in a `backofficePage`, `enterPreview(homeKey)` using that page's context (`ManagementApi` built from `page.context().request` or the same storage state, as long as the `UMB_PREVIEW` cookie lands in the page's context). Two loads of `/en/` 1.5 s apart, expect `cached` to differ.
- [ ] **Step 2: Run to verify.** `UMBRACO_VERSIONS=17 npm test -- specs/our-cache.spec.ts`. Expected: the optOut tests FAIL (sample text not changed yet).
- [ ] **Step 3: Change the sample text.** If republishing Home doesn't change `UpdateDate` on some version, record a Ruling and change the header through `PUT /document/{id}` before publishing.
- [ ] **Step 4: Run all versions.** `npm test -- specs/our-cache.spec.ts`. Expected: PASS on 16–19.
- [ ] **Step 5: Commit.** `git commit -m "Test our-cache clearing on publish, opt-out, vary-by, debug and preview"`.

### Task 5: `our-form` submission and `umb-area`

**Files:**
- Modify: `Our.Umbraco.TagHelpers.TestSite/Controllers/ContactFormController.cs`. On valid input, set `TempData["ContactFormSubmitted"] = model.Name` before `RedirectToCurrentUmbracoPage()`.
- Create: `Our.Umbraco.TagHelpers.TestSite/Controllers/AreaContactFormController.cs`. `[PluginController("TagHelpersDemo")] public class AreaContactFormController : SurfaceController`, with `[HttpPost] IActionResult Submit(string name)` that sets `TempData["AreaFormSubmitted"] = name` and returns `RedirectToCurrentUmbracoPage()`.
- Modify: `.../taghelper-samples/SurfaceControllerForm.cshtml`. Show `<p class="form-success">Thanks @name, your message was sent.</p>` when `TempData["ContactFormSubmitted"]` is set. Add a second form `id="testAreaForm"` with `our-controller="AreaContactForm" our-action="Submit" umb-area="TagHelpersDemo" method="post"`, one `<input name="name">`, a submit button, and `<p class="area-form-success">Area form received @name</p>` when `TempData["AreaFormSubmitted"]` is set. Add a short explanation of `umb-area` in the sample text.
- Test: `tests/e2e/specs/our-form.spec.ts`

- [ ] **Step 1: Write the failing tests.**
  - `invalid input shows the validation messages`: submit `#testContactForm` empty, expect at least 3 non-empty `span.field-validation-error` in the section and no `.form-success`.
  - `valid input reaches the surface controller and redirects back`: fill Name `E2E Tester`, Email `e2e@example.com`, Message `Hello from Playwright`, submit, expect URL `/en/` and `.form-success` text `Thanks E2E Tester, your message was sent.`
  - `umb-area routes to the surface controller in that area`: fill and submit `#testAreaForm` with `E2E Area`, expect `.area-form-success` text `Area form received E2E Area`.
- [ ] **Step 2: Run to verify it fails.** `UMBRACO_VERSIONS=17 npm test -- specs/our-form.spec.ts`. Expected: the valid and area tests FAIL (no success message or form).
- [ ] **Step 3: Implement** the controller, area controller and sample changes above.
- [ ] **Step 4: Run all versions.** Expected: PASS on 16–19.
- [ ] **Step 5: Commit.** `git commit -m "Demo and test our-form submissions, including umb-area"`.

### Task 6: `our-svg` class, cache and ignore-appsettings

**Files:**
- Modify: `Our.Umbraco.TagHelpers.TestSite/appsettings.json`. Add `"Our.Umbraco.TagHelpers": { "OurSVG": { "EnsureViewBox": true } }`. Keep the file's CRLF line endings.
- Create: `Our.Umbraco.TagHelpers.TestSite/wwwroot/samples/cache-demo.svg`. A small SVG with `width="24" height="24"` and `<title>original</title>`.
- Modify: `.../taghelper-samples/our-svg.cshtml`. Replace the `TODO` list with samples, each wrapped in `<div data-demo="…">`:
  - `class`: `partying-face.svg` with `class="demo-svg demo-svg--large"`.
  - `appsettings`: `partying-face.svg` with no attributes.
  - `ignore-appsettings`: `partying-face.svg` with `ignore-appsettings="true"`.
  - `cached`: `cache-demo.svg` with `cache="true" cache-minutes="5"`.
  - `uncached`: `cache-demo.svg`.
  - Add a line of explanation for each.
- Test: `tests/e2e/specs/our-svg.spec.ts`

**Interfaces:** Needs the site's published path. Add `siteRoot(umbracoMajor: number): string` → `.data/v<N>/app` to `fixtures.ts`.

- [ ] **Step 1: Write the failing tests.** Change the existing two tests to select only the original samples (`:scope > div:not([data-demo]) > svg`). Add:
  - `class is added to the inlined svg`: `[data-demo="class"] svg` has class matching `/demo-svg demo-svg--large/`.
  - `appsettings EnsureViewBox applies without the attribute`: `[data-demo="appsettings"] svg` has `viewbox` (read with `getAttribute('viewBox') ?? getAttribute('viewbox')`) `0 0 246 246` and no `width` attribute.
  - `ignore-appsettings skips the site settings`: `[data-demo="ignore-appsettings"] svg` has `width` `246` and no viewbox.
  - `cache serves the cached svg after the file changes`: load `/en/`, then in `try` overwrite `siteRoot(major)/wwwroot/samples/cache-demo.svg`, replacing `original` with `changed`. Reload; expect `[data-demo="cached"] title` to be `original` and `[data-demo="uncached"] title` to be `changed`. In `finally`, restore the original content.
- [ ] **Step 2: Run to verify it fails.** `UMBRACO_VERSIONS=17 npm test -- specs/our-svg.spec.ts`. Expected: the new tests FAIL (no `data-demo` samples).
- [ ] **Step 3: Implement** the appsettings, SVG file and samples.
- [ ] **Step 4: Run all versions.** Expected: PASS on 16–19, including the two existing tests.
- [ ] **Step 5: Commit.** `git commit -m "Demo and test our-svg class, cache and ignore-appsettings"`.

### Task 7: `our-img` class, style, height and large breakpoints

**Files:**
- Modify: `.../taghelper-samples/our-img.cshtml`. Append two rows at the end of the table, in the existing Input/Output/Rendered layout:
  - "Class and style": `<our-img src="/samples/cute-unicorn.jpg" width="300" height="150" class="demo-img demo-img--rounded" style="border: 2px solid green" />`.
  - "Large breakpoints with crops": `<our-img media-item="Model.Image" width="200" height="200" width--l="500" height--l="250" width--xl="600" cropalias--xl="desktop" width--xxl="800" cropalias--xxl="desktop" />`.
- Test: `tests/e2e/specs/our-img.spec.ts`

- [ ] **Step 1: Write the failing tests.**
  - `class and style are applied to the img`: `img[src^="/samples/cute-unicorn.jpg?width=300"]` has class matching `/demo-img demo-img--rounded/`, `style` matching `/border: 2px solid green/`, `width` `300` and `height` `150`.
  - `large breakpoints render sources with the large media queries`: the last `picture` has `source[media="(min-width: 992px)"]` with width `500` and height `250`, `source[media="(min-width: 1200px)"]` with width `600`, and `source[media="(min-width: 1400px)"]` with width `800`. The 1200 and 1400 sources have `srcset` matching `` new RegExp(`^${mediaFile}\\?cc=`) ``.
  - The existing `every rendered image URL resolves` covers the new URLs.
- [ ] **Step 2: Run to verify it fails.** `UMBRACO_VERSIONS=17 npm test -- specs/our-img.spec.ts`. Expected: FAIL (rows missing).
- [ ] **Step 3: Implement** the two sample rows.
- [ ] **Step 4: Run all versions.** If only the `style` assertion fails (Review Focus 4), the library drops `style` unless `ApplyAspectRatio` is on. Fixing it is outside this plan's scope (library behaviour). Split that assertion into its own `test.fixme('keeps the style attribute', …)` with a comment, record a Ruling, and draft an issue in the scratchpad for Warren to approve (don't create it). Expected: everything else PASSES on 16–19.
- [ ] **Step 5: Commit.** `git commit -m "Demo and test our-img class, style, height and large breakpoints"`.

### Task 8: `our-fallback` ancestors and tag content on a page

**Files:**
- Create: `Our.Umbraco.TagHelpers.TestSite/uSync/site/Content/fallback-demo.config`. Copy `page-aa.config` with a new random `Key`, `Alias="Fallback Demo"`, `NodeName Default="Fallback Demo"`, `Path /Home/PageA/FallbackDemo`, `SortOrder` 2, and an empty header value (`<Value><![CDATA[]]></Value>`). Keep the file's encoding and line endings like its neighbours.
- Create: `.../taghelper-samples/our-fallback-page.cshtml` (`@inherits UmbracoViewPage<Page>`) with:
  - `<h3 data-demo="ancestors"><our-fallback property="Header" mode="Fallback.ToAncestors">No header here or on any ancestor</our-fallback></h3>`
  - `<h3 data-demo="content"><our-fallback property="Header" mode="Fallback.ToDefaultValue">This page has no header, so the tag content is shown</our-fallback></h3>`
  - Short text explaining each mode.
- Modify: `Views/Page.cshtml`. Add `<section data-sample="our-fallback">` with that partial and `model="Model"`.
- Test: `tests/e2e/specs/our-fallback.spec.ts`

- [ ] **Step 1: Write the failing tests.**
  - `ancestors mode uses the nearest ancestor value`: `/en/page-a/fallback-demo/` → `[data-demo="ancestors"]` has text `Page A`.
  - `uses the page's own value when it has one`: `/en/page-a/` → `[data-demo="ancestors"]` has text `Page A` and `[data-demo="content"]` has text `Page A`.
  - `falls back to the tag content when there is no value`: `/en/page-a/fallback-demo/` → `[data-demo="content"]` has text `This page has no header, so the tag content is shown`.
- [ ] **Step 2: Run to verify it fails.** `UMBRACO_VERSIONS=17 npm test -- specs/our-fallback.spec.ts`. Expected: FAIL (page 404 / section missing).
- [ ] **Step 3: Implement.** If uSync rejects the content file or the page isn't published on a fresh boot, check `.data/v<N>/site.log` for the uSync import report before changing the file.
- [ ] **Step 4: Run all versions,** then run the whole suite once on 17 (`UMBRACO_VERSIONS=17 npm test`). The new page appears in the demo's navigation, so this checks it breaks no other spec. Expected: PASS.
- [ ] **Step 5: Commit.** `git commit -m "Demo and test our-fallback ancestors and tag content on a page"`.

### Task 9: `our-self-host` href

**Files:**
- Modify: `.../taghelper-samples/our-self-host.cshtml`. Add an "href example" heading with text and `<link rel="stylesheet" href="https://unpkg.com/normalize.css@8.0.1/normalize.css" our-self-host />`.
- Test: `tests/e2e/specs/our-self-host.spec.ts`

- [ ] **Step 1: Write the failing test.** `rewrites href to a locally hosted copy and serves it`: the section's `link[rel="stylesheet"]` has `href` `/assets/normalize.css@8.0.1/normalize.css` and `data-original-href` `https://unpkg.com/normalize.css@8.0.1/normalize.css`. `request.get(href)` returns 200 and a body containing `normalize.css`.
- [ ] **Step 2: Run to verify it fails.** Expected: FAIL (no link element).
- [ ] **Step 3: Implement** the sample.
- [ ] **Step 4: Run all versions.** Expected: PASS.
- [ ] **Step 5: Commit.** `git commit -m "Demo and test our-self-host on an href"`.

### Task 10: Full verification and docs

**Files:** Modify `README.md` only if its demo/tests section lists covered scenarios or test identities. Add one line: setup seeds an Editor user, a no-content user and a Staff member.

- [ ] **Step 1:** Stop all sites. Run `npm test` (all versions, fresh sites). Expected: everything passes. Skipped: the `our-active-class` fixme ×4, plus the `our-img` style fixme ×4 if Task 7 needed it.
- [ ] **Step 2:** `cd Our.Umbraco.TagHelpers.Tests && dotnet test` (unit tests, net9/10/11). Expected: 42/42 per framework.
- [ ] **Step 3:** Commit the README change, if any: `git commit -m "Document the identities seeded by the e2e setup"`.
