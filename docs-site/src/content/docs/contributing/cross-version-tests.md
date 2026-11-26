---
title: Cross-version tests
description: Run the unit tests and the Playwright suite that checks every sample on Umbraco 16 to 19.
sidebar:
  order: 2
---

Any change to the package must build on every target framework and pass the unit tests and the cross-version tests on all four Umbraco versions.

## Unit tests

```bash
dotnet test Our.Umbraco.TagHelpers.Tests
```

They run on every target framework (`net9.0`, `net10.0` and `net11.0`).

## Cross-version tests

`tests/e2e` contains [Playwright](https://playwright.dev) tests that publish the demo site for each Umbraco version, start it with a fresh database and check every tag helper sample renders as expected. They run in GitHub Actions on every pull request (all versions on Linux, plus Umbraco 17 on Windows and macOS), and locally on Windows, macOS and Linux.

You need Node.js LTS and the .NET runtimes for the versions you test (.NET 9 for Umbraco 16, .NET 10 for 17 and 18, .NET 11 for 19).

```bash
cd tests/e2e
npm ci                           # first time only
npx playwright install chromium  # first time only

npm test                                         # builds the library, then tests all versions
npm test -- specs/our-img.spec.ts                # a single spec
npm test -- specs/our-form.spec.ts -g "umb-area" # tests whose name matches
npm run test:package                             # the same tests against the packed NuGet package
npm run report                                   # HTML report, with screenshots and traces of failures
```

### Choosing versions

Set `UMBRACO_VERSIONS`:

| Shell | Command |
|---|---|
| bash / zsh (macOS, Linux, Git Bash) | `UMBRACO_VERSIONS=17,19 npm test` |
| PowerShell | `$env:UMBRACO_VERSIONS="17,19"; npm test` (clear it with `Remove-Item Env:UMBRACO_VERSIONS`) |
| Command Prompt | `set UMBRACO_VERSIONS=17,19 && npm test` |

### Good to know

- Each run publishes the sites into `tests/e2e/.data/` with throwaway databases, so it never touches the demo site's own database. Site logs are in `tests/e2e/.data/vN/site.log`.
- Sites start on ports 5016 to 5019. If one of those ports is still in use (for example by a site from an interrupted run), Playwright stops with "is already used": stop that process and run again.
- While iterating on specs only, set `E2E_REUSE_SITES=1` to reuse sites that are already running. They keep serving the views they were published with.
- To step through tests in Playwright's UI mode, run `npm run build:library` and then `npx playwright test --ui`, with `UMBRACO_VERSIONS` set to the version you want.
- `npm test` builds the package once before the sites are published. Running `npx playwright test` directly also works, but a site refuses to start if the package build is missing or older than its source, so run `npm run build:library` first.

### Testing the packed package

`npm test` runs the sites against the `Our.Umbraco.TagHelpers` project. `npm run test:package` tests what would be published instead: it packs the library like `build.ps1`, installs that `.nupkg` into every site, checks each site loads the tag helpers and the appsettings schema from the package, then runs the same tests.

The package gets a unique prerelease version (such as `2.1.0-e2e.20261115093000`), because NuGet caches packages by version. To test a package you've already packed, set `E2E_PACKAGE_DIR` to the folder holding its `.nupkg`.

## In GitHub Actions

Pull requests and pushes to `v2/dev` run both modes. Each run packs the package once with a CI version, `<version>-ci.<run number>` (such as `2.1.0-ci.42`, which NuGet sorts below `2.1.0-rc1` and `2.1.0`), and attaches the `.nupkg` and `.snupkg` (symbols) to the run.

### Trying a pull request's package

On the run's summary page in GitHub Actions, download `Our.Umbraco.TagHelpers.<version>.nupkg` from the Artifacts section into a folder, such as `./nuget-package`. Then use that folder as a package source in your site and install the version in the file name:

```bash
dotnet add package Our.Umbraco.TagHelpers --version 2.1.0-ci.42 --source ./nuget-package
```

## Known bugs

A known bug stays in the tests as `test.fixme`, with a comment linking its GitHub issue, so it's tracked and starts failing loudly once it's fixed.
