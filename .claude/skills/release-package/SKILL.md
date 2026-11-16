---
name: release-package
description: Use when preparing a release of the Our.Umbraco.TagHelpers NuGet package (v2.x.x) — choosing the next version number, checking for breaking API changes since the last release, checking for prerelease Umbraco dependencies (NU5104), packing with build.ps1, or drafting the GitHub release notes.
---

# Release the package

Prepares a release and hands the user everything to publish it. **You never publish.** The user tags, creates the GitHub release and pushes to NuGet.

Releases are cut from `v2/dev` once the work's PRs are merged (tags `vX.Y.Z`, GitHub release notes generated from PR labels by `.github/release.yml`). Do the prep on a branch off `v2/dev` (for example `v2/release/X.Y.Z`) so the version bump goes in by PR like everything else.

## 1. What changed

```bash
git fetch origin --tags
git describe --tags --abbrev=0 origin/v2/dev     # last release, e.g. v2.0.0
git log --oneline <last-tag>..HEAD
```

## 2. Breaking API changes (mechanical, don't eyeball the diff)

Pack with the SDK's package validation against the last released version (without the `v`):

```bash
dotnet pack Our.Umbraco.TagHelpers -c Release -o <scratch-dir> -p:EnablePackageValidation=true -p:PackageValidationBaselineVersion=<last-version> -nologo
```

- Every `CP0001`/`CP0002`/... error is a public API that existed in the last release and is gone or changed now (removed members, changed constructors, removed types). Record each one verbatim for the release notes.
- It only compares target frameworks the baseline package has. A new target framework has no baseline, which is fine.
- Never make it pass by generating a suppression file (`ApiCompatGenerateSuppressionFile`) or by leaving validation switched on in the `.csproj`. It's a check you run, not part of the build.

## 3. Prerelease dependencies

```bash
grep -nE 'Umbraco\.Cms\.[A-Za-z.]+" Version="[^"]*-' Our.Umbraco.TagHelpers/Our.Umbraco.TagHelpers.csproj
```

Any match (for example `19.0.0-rc1`) means the release **must** be a prerelease (`X.Y.Z-rcN`): a stable package can't depend on a prerelease Umbraco (`dotnet pack` warns `NU5104`, and consumers on the stable Umbraco would get the RC). The other option is to wait for the stable Umbraco and move to it first (the `umbraco-version-support` skill, "Moving from RC to stable").

## 4. Propose the version, the user decides

Using steps 1 to 3, propose a SemVer version and say why:
- breaking API changes (step 2): major, by SemVer. The maintainer may accept small breaks in a minor release; list them so it's an informed choice.
- new Umbraco versions, target frameworks or features: minor.
- fixes only: patch.
- plus the `-rcN` suffix if step 3 found a prerelease dependency.

Ask the user to confirm the version before going on.

## 5. Prove it

All must pass on the current code. Report the real counts:

```bash
dotnet build Our.Umbraco.TagHelpers.slnx                 # 0 errors, 0 warnings
dotnet test Our.Umbraco.TagHelpers.Tests                 # every target framework
cd tests/e2e && npm test && npm run test:package         # every Umbraco version, project and packed .nupkg
```

## 6. Set the version and pack

1. Set `<Version>` in `Our.Umbraco.TagHelpers/Our.Umbraco.TagHelpers.csproj`. Commit it on its own ("Set the version to X.Y.Z"), no `Co-Authored-By` trailer.
2. `pwsh ./build.ps1` packs into `build.out/`. It must contain `Our.Umbraco.TagHelpers.X.Y.Z.nupkg` and `.snupkg`, and nothing older.
3. For a stable version, the pack output must have no `NU5104`.
4. Check the dependencies the package declares in `Our.Umbraco.TagHelpers/obj/Release/Our.Umbraco.TagHelpers.X.Y.Z.nuspec`: one `<group>` per target framework (`net9.0`, `net10.0`, `net11.0`, ...), each with only `Umbraco.Cms.Core`, `Umbraco.Cms.Web.Common` (that framework's lowest supported Umbraco) and `Stubble.Core`. Anything else is a stray reference.

## 7. Draft the release notes

GitHub generates the list of PRs. Write only the part that goes above it, to a file in your scratchpad:

```markdown
## Highlights
<one or two sentences: what this release is for>

## Supported versions
| Umbraco | .NET | Target framework |
|---|---|---|
(from the mapping comment at the top of the library .csproj)

## Breaking changes
- <each API from step 2, in plain words, with what to use instead>

## Upgrading
<only if something is needed beyond updating the package, e.g. new appsettings or constructor changes for people who subclass or new up the tag helpers>
```

Leave out sections with nothing in them. Show the draft to the user. If a merged PR introduced a breaking change but has no `breaking-change` label, tell the user, so the generated list files it under "Breaking Changes".

## 8. Hand over

Give the user the commands to run themselves after the release branch's PR is merged into `v2/dev`, and stop:

```bash
git checkout v2/dev && git pull
pwsh ./build.ps1                       # pack from the merged v2/dev
gh release create vX.Y.Z --target v2/dev --draft --generate-notes --notes-start-tag <last-tag> -F <notes-file>   # add --prerelease for -rcN
dotnet nuget push build.out/Our.Umbraco.TagHelpers.X.Y.Z.nupkg --source https://api.nuget.org/v3/index.json --api-key <key>
```

`--draft` lets them review the release on GitHub before publishing it, which also creates the tag. `dotnet nuget push` also pushes the `.snupkg` next to the `.nupkg`.

## Never

- Push, tag, create a GitHub release, or push to NuGet, even when asked to "release": prepare it and hand over the commands. A version on NuGet can't be reused or deleted, only unlisted.
- Release a stable version with a prerelease Umbraco dependency.
- Hide a breaking change: every API from step 2 goes in the notes.
- Report tests as passing without having run them in this session.
