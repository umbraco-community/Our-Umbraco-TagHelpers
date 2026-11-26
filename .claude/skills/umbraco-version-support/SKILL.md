---
name: umbraco-version-support
description: Use when adding support for a new Umbraco major (e.g. 20, 21) or release candidate, a new .NET version (net12.0, ...), moving an Umbraco major from RC to stable, or dropping an old Umbraco major in Our.Umbraco.TagHelpers v2. Walks the version checklist, updates every version-specific file, and proves the change with the unit tests and the cross-version Playwright suite.
---

# Umbraco version support

The procedure and the list of files live in `docs/supporting-a-new-umbraco-version.md`. **Read it first.** It is the source of truth, and this skill only says how to run it.

## Before you start

1. Confirm what's asked: add Umbraco `N` (stable or RC), add .NET `X`, RC → stable, or drop `N`.
2. Work on a new branch off `v2/dev` (for example `v2/feature/umbraco-N`). Never commit to `main` or merge `v2/dev` into it. Don't push, and don't open a PR unless asked.
3. Look up current versions; don't rely on memory:
   - the Umbraco.Cms versions on NuGet (`https://api.nuget.org/v3-flatcontainer/umbraco.cms/index.json`) and the target framework of the one you'll use (its `.nuspec`),
   - the uSync versions (`https://api.nuget.org/v3-flatcontainer/usync/index.json`),
   - for API changes, the Umbraco docs (through the `ctx7` CLI if you have it) and the version's release notes or breaking-changes list.
4. Check the SDK and runtime are installed (`dotnet --list-sdks`, `dotnet --list-runtimes`). If not, ask the user to install them. Don't install SDKs yourself.

## Steps

Follow the checklist's sections in order, one commit per step:

1. **Library**: target framework and Umbraco references. Then `dotnet build Our.Umbraco.TagHelpers -c Release` must build every target framework with 0 errors.
2. **Unit tests**: target framework, and any `#if` for changed constructors. `dotnet test Our.Umbraco.TagHelpers.Tests` must pass on every framework. Report the pass count per framework.
3. **Demo site**: the `UmbracoVersion` block and the error text. `dotnet build Our.Umbraco.TagHelpers.TestSite -p:UmbracoVersion=N` must build.
4. **E2E harness and CI**: `playwright.config.ts`, `scripts/start-site.mjs`, `.github/workflows/e2e.yml` (with `continue-on-error` while it's an RC), `.github/workflows/codeql-analysis.yml`.
5. **Run the cross-version suite**: in `tests/e2e`, first `UMBRACO_VERSIONS=N npm test`, then `npm test` for every version, then `npm run test:package` (the same suite against the packed `.nupkg`). Fix failures using the checklist's "When something fails" guide:
   - Library fixes must keep every other version passing. A runtime `MissingMethodException` on a version that shares a target framework means building against the older version and avoiding the moved member.
   - Commit each fix separately, with a unit test when the fix is in the library.
   - A failure that's a genuine library bug outside the version work: mark the test `test.fixme` with a comment, draft a GitHub issue for the user, and ask before creating it.
6. **Docs**: the supported versions tables in `README.md`, `umbraco-marketplace-readme.md` and the docs site's installation page (`docs-site/src/content/docs/getting-started/installation.md`), and the `-p:UmbracoVersion` list and prerequisites on its contributing pages.
7. **Checklist upkeep**: add what broke this time to "What broke last time" in `docs/supporting-a-new-umbraco-version.md`.

## Rules

- **Cross-platform**: scripts and documented commands must work on Windows, macOS and Linux. Node scripts spawn `dotnet` without a shell and build paths with `path.join`. Where a command needs an environment variable, document bash, PowerShell and Command Prompt forms (see the README).
- **Fresh sites**: Playwright refuses to reuse running sites unless `E2E_REUSE_SITES=1`. If a port 50N is busy, stop the leftover site rather than reusing it.
- **Commits**: granular, and no `Co-Authored-By: Claude` trailer.
- **Evidence**: report the actual pass/fail/skip counts from the runs, and every `fixme` you added. Never say the suite passes without having run it.
