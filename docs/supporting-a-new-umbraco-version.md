# Supporting a new Umbraco or .NET version

Our.Umbraco.TagHelpers v2 ships **one NuGet package for several Umbraco majors**. Each .NET version gets its own build (target framework) in the package, compiled against the Umbraco version that runs on it:

| Target framework | Umbraco | Referenced (minimum) Umbraco version |
|---|---|---|
| `net9.0` | 16 | 16.0.0 |
| `net10.0` | 17 (LTS), 18 | 17.0.0 |
| `net11.0` | 19 | 19.0.0-rc2 |

Because Umbraco 17 and 18 share `net10.0`, the 17-built DLL also has to work on 18. That's binary compatibility, not just source compatibility: a member that moves between versions breaks at runtime even when the code compiles (see [What broke last time](#what-broke-last-time)). The cross-version Playwright suite exists to catch exactly that.

Use this checklist when:
- a new Umbraco major comes out (or reaches RC),
- a new .NET version (`net12.0`, ...) is needed,
- an Umbraco major moves from RC to stable,
- an old Umbraco major is dropped.

Work on a branch off `v2/dev` and open a PR into `v2/dev`. Make one commit per logical step.

## 1. Find the versions

For the new Umbraco major `N`, find on nuget.org:
- the **Umbraco.Cms** version (latest stable, or the RC) and the .NET version it targets,
- the matching **uSync** version (the demo site imports its content with uSync; uSync releases per Umbraco major).

Install that .NET SDK and runtime locally. `dotnet --list-sdks` and `dotnet --list-runtimes` show what you have.

## 2. Update every version-specific place

| File | Change |
|---|---|
| `Our.Umbraco.TagHelpers/Our.Umbraco.TagHelpers.csproj` | Add the target framework to `<TargetFrameworks>` (only if `N` needs a new .NET), plus an `ItemGroup` for it referencing `Umbraco.Cms.Core` and `Umbraco.Cms.Web.Common` at the **lowest** version of that major (so the package works on every patch). Update the mapping comment at the top. |
| `Our.Umbraco.TagHelpers.Tests/Our.Umbraco.TagHelpers.Tests.csproj` | Add the new target framework. |
| `Our.Umbraco.TagHelpers.TestSite/Our.Umbraco.TagHelpers.TestSite.csproj` | Add a `PropertyGroup Condition="'$(UmbracoVersion)' == 'N'"` with `TargetFramework`, `UmbracoCmsVersion` (latest patch) and `USyncVersion`. Update the `ValidateUmbracoVersion` error text and the comment at the top. |
| `tests/e2e/playwright.config.ts` | Add `N` to `supported`. The site runs on port `50N`. |
| `tests/e2e/scripts/start-site.mjs` | Add `N: <dotnet major>` to `dotnetMajorFor`. |
| `.github/workflows/e2e.yml` | Add `N` to the matrix, add the .NET version to both `setup-dotnet` steps (the `pack` job and the `e2e` job). While `N` is an RC, set `continue-on-error: ${{ matrix.umbraco == N }}` (report, don't block). |
| `.github/workflows/codeql-analysis.yml` | Add the .NET version to `setup-dotnet` (CodeQL builds every target framework). |
| `README.md`, `umbraco-marketplace-readme.md` | The supported versions table. |
| `docs-site/src/content/docs/getting-started/installation.md` | The supported versions table. |
| `docs-site/src/content/docs/contributing/` | The `-p:UmbracoVersion=` list and the runtime prerequisites (`demo-site.md`, `cross-version-tests.md`). |

The demo default stays on the current **LTS** (`UmbracoVersion` default in `Our.Umbraco.TagHelpers.TestSite/Directory.Build.props`). Move it only when a newer LTS ships.

## 3. Build, test, fix

From the repository root:

```bash
dotnet build Our.Umbraco.TagHelpers -c Release        # every target framework
dotnet test Our.Umbraco.TagHelpers.Tests              # unit tests on every target framework
dotnet build Our.Umbraco.TagHelpers.TestSite -p:UmbracoVersion=N
```

Then the cross-version suite (see the docs site's "Cross-version tests" page for setting `UMBRACO_VERSIONS` in PowerShell or Command Prompt):

```bash
cd tests/e2e
UMBRACO_VERSIONS=N npm test     # the new version first
npm test                        # then every version
npm run test:package            # then every version against the packed .nupkg
```

`npm run test:package` catches what only shows up in a real install: the wrong `lib/` folder, a dependency that doesn't restore on one version, or the package's `buildTransitive` schema registration.

When something fails:
- **A compile error in the library**: an API changed. Prefer a change that compiles on every version. Use `#if NETx_0_OR_GREATER` only when the API genuinely differs per target framework.
- **`MissingMethodException` / `TypeLoadException` at runtime** on a version that shares a target framework with another (like 17 and 18): a member moved between types. Build against the older version and avoid the moved member (for example, use an extension method or access it through the interface that still declares it).
- **A demo sample fails**: the sample may rely on behaviour that changed (see below). Fix the sample so it works on every version.
- **A test helper fails** (login, publishing, preview in `tests/e2e/fixtures.ts` and `tests/e2e/umbraco-api.ts`): the backoffice or Management API changed. The OpenAPI document is at `/umbraco/openapi/management.json` (18+) or `/umbraco/swagger/management/swagger.json` (16, 17) on a running site.
- **The demo people aren't created**: check `tests/e2e/.data/vN/site.log` for `Demo people:` lines from `Our.Umbraco.TagHelpers.TestSite/Migrations/AddDemoPeople.cs`.

Every site's log is at `tests/e2e/.data/vN/site.log`, and `npm run report` shows screenshots and traces of failures.

## 4. Moving from RC to stable

- Bump the references to the stable version: the library `ItemGroup`, the TestSite `PropertyGroup` and the uSync version.
- Remove `continue-on-error` for `N` in `.github/workflows/e2e.yml`.
- Don't release a stable package while it still references a prerelease Umbraco. `dotnet pack` warns about this (`NU5104`).
- To release, follow `.claude/skills/release-package/SKILL.md` (the `release-package` skill): it checks for breaking changes and prerelease dependencies, runs every test, sets `<Version>`, packs and drafts the release notes.

## 5. Dropping an old version

Remove it from all the places in step 2. Drop its target framework only if no remaining Umbraco version uses it, and remove `#if` branches that only existed for it.

## What broke last time

Keep this list up to date. It's where to look first.

- **17 → 18:** `Id`, `Name` and other members moved from `IPublishedContent` to `IPublishedElement`. A DLL built against 17 failed on 18 with `MissingMethodException: IPublishedContent.get_Name()`. Fixed in `ImgTagHelper` by using the `Name()` extension and reading values through `IPublishedContent`.
- **19:**
  - `AsEnumerableOfOne` was removed.
  - `Constants.Security.AllowedApplicationsClaimType` was removed (`EditLinkTagHelper` now uses `IUserService` and `AllowedSections`).
  - `MediaFileManager` gained a `Lazy<ICoreScopeProvider>` constructor parameter (`#if NET11_0_OR_GREATER` in `InlineSvgTagHelperTests`).
  - MediaPicker3 always returns a list (the `our-svg` sample handles both).
  - The Management API user group model gained required fields.
- **Backoffice and API between versions:**
  - 16 keeps the backoffice token in `localStorage`; 17+ uses an httpOnly cookie and the literal `Bearer [redacted]` header.
  - `POST /preview` was removed in 18 (use the document's `preview-url`).
  - The OpenAPI document moved in 18.
  - Concurrent first logins on a fresh 16 site can fail with "The specified 'client_id' is invalid", so the test setup logs in one user at a time.
- **Umbraco 17:** `UseHttps` defaults to true (the test sites set `Umbraco__CMS__Global__UseHttps=false`).
- **uSync on 18 and 19:** the first-boot import warns `MemberTypeHandler ... Cannot change the Key of an existing PropertyType`. This is harmless.
