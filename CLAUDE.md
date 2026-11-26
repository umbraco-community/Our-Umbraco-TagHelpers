# Our.Umbraco.TagHelpers

Community NuGet package of ASP.NET Core Razor tag helpers for Umbraco. Version 2 is **one package for Umbraco 16, 17, 18 and 19**, multi-targeted `net9.0` (16), `net10.0` (17 LTS and 18) and `net11.0` (19, release candidate).

## Layout

- `Our.Umbraco.TagHelpers/`: the package (tag helpers, services, configuration). This is the only thing that ships.
- `Our.Umbraco.TagHelpers.Tests/`: NUnit unit tests, run on every target framework.
- `Our.Umbraco.TagHelpers.TestSite/`: demo site with a sample of every tag helper (`Views/Partials/taghelper-samples/`). Runs Umbraco 17 by default; `-p:UmbracoVersion=16|17|18|19` switches version. Content comes from uSync (`uSync/site/`, imported on first boot). Demo users and members come from a TestSite package migration (`Migrations/AddDemoPeople.cs`, logins in `appsettings.Development.json`).
- `tests/e2e/`: Playwright suite that publishes and boots the demo site for each Umbraco version and checks every sample. It appears in the solution through `tests/e2e/Our.Umbraco.TagHelpers.E2E.csproj`, a `Microsoft.Build.NoTargets` project that builds nothing and picks up new files automatically.
- `docs-site/`: the public documentation site (Astro Starlight), published to https://umbraco-community.github.io/Our-Umbraco-TagHelpers/ from `v2/dev` by `.github/workflows/docs.yml`. One page per tag helper in `src/content/docs/tag-helpers/`; the release notes and contributors pages are built from GitHub (releases and commit authors) at build time, and the site rebuilds on every push to `v2/dev`, each published release and weekly, so they stay current with nothing to update by hand. It's in the solution through `docs-site/Our.Umbraco.TagHelpers.Docs.csproj`, another `NoTargets` project.
- `Our.Umbraco.TagHelpers.slnx`: `.slnx` can't include folders, so single files at the root (workflows, `build.ps1`, docs, this file) are listed one by one in the `/Build/` and `/Docs/` solution folders. Add new ones there.
- `docs/supporting-a-new-umbraco-version.md`: checklist for adding, promoting or dropping Umbraco/.NET versions. Use the `umbraco-version-support` skill (`.claude/skills/`) to run it.
- Releases: the `release-package` skill prepares one (breaking-change and prerelease-dependency checks, tests, version, pack, release notes). The user tags, creates the GitHub release and pushes to NuGet.
- `docs/superpowers/`: design docs and implementation plans from earlier work.

## Commands

All from the repository root unless noted. They work on Windows, macOS and Linux.

```bash
dotnet build Our.Umbraco.TagHelpers -c Release            # every target framework
dotnet test Our.Umbraco.TagHelpers.Tests                  # unit tests, every target framework
dotnet run --project Our.Umbraco.TagHelpers.TestSite      # demo site (https://localhost:44302)

cd tests/e2e && npm test                                  # e2e, all versions (builds the library first)
cd tests/e2e && npm test -- specs/our-img.spec.ts         # one spec
cd tests/e2e && npm run test:package                      # e2e against the packed .nupkg (what gets published)
cd tests/e2e && E2E_PACKAGE_DIR=<folder> npm run test:package   # e2e against an already packed .nupkg
pwsh ./build.ps1                                          # pack into build.out/ (version from the library .csproj)
cd docs-site && npm run dev                               # docs site (http://localhost:4321/Our-Umbraco-TagHelpers/)
cd docs-site && npm run build && npm run check            # build and type check the docs site
```

Choose e2e versions with `UMBRACO_VERSIONS=17,19`. The docs site's "Cross-version tests" page (`docs-site/src/content/docs/contributing/cross-version-tests.md`) shows the PowerShell and Command Prompt forms. Site logs are in `tests/e2e/.data/vN/site.log`.

## Conventions

- **Branches**: `v2/dev` is the long-lived branch for v2.x.x releases. Never merge it into `main`. Do work on a branch off `v2/dev` and PR it back into `v2/dev`.
- **Commits**: small and granular, with no `Co-Authored-By: Claude` trailer. Don't push or open PRs unless asked.
- **Cross-version**: any library change must build on every target framework and pass the unit and e2e suites on all four versions. Umbraco 17 and 18 share `net10.0`, so the 17-built DLL must also work on 18 at runtime (watch for members that move between interfaces).
- **Cross-platform**: scripts and documented commands must work on Windows, macOS and Linux.
- **Demo samples**: every sample sits in `<section data-sample="name">` in `Views/Home.cshtml` or `Views/Page.cshtml`, which the specs select by. A new tag helper option gets a sample and a spec. Test data the specs need goes in uSync (content) or the TestSite migration (people), never only in a local database.
- **Docs site**: a new tag helper gets a page in `docs-site/src/content/docs/tag-helpers/` with `addedIn` (the version it ships in) and `usage` in its frontmatter; a new option gets an inline `<VersionBadge version="x.y.z" inline />` (in an `.mdx` page). Badges and the sidebar's "Coming soon"/"New" work themselves out from the GitHub releases. See the site's "Writing the docs" page. The docs site is the reference: `README.md` (also the NuGet readme) and `umbraco-marketplace-readme.md` (the Umbraco Marketplace listing) only list the tag helpers with links to their pages, using absolute URLs, so a new tag helper adds a line to both, and a new option only needs the docs page.
- **Known bugs** stay as `test.fixme` with a comment linking the GitHub issue (for example #97, `our-active-class` with culture domains).
- **Docs**: before relying on Umbraco or library APIs, check current docs (the `ctx7` CLI if available); Umbraco APIs change between majors.
