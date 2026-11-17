---
title: Packing the package
description: Pack the NuGet package with build.ps1 and try a local build in the demo site.
sidebar:
  order: 3
---

`build.ps1` packs the package into `build.out/`. It runs in Windows PowerShell or [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) (`pwsh` on macOS and Linux).

```bash
pwsh ./build.ps1
```

The version is the `<Version>` in `Our.Umbraco.TagHelpers/Our.Umbraco.TagHelpers.csproj`. Pass `-Version` to override it:

```bash
pwsh ./build.ps1 -Version 2.1.0-rc1
```

## Trying a packed build in the demo site

Pack it with its own version, then run the demo site against it:

```bash
pwsh ./build.ps1 -Version 2.1.0-local.1
dotnet run --project Our.Umbraco.TagHelpers.TestSite -p:TagHelpersSource=Package -p:TagHelpersPackageVersion=2.1.0-local.1
```

Use a new version for each new build of the package, because NuGet caches packages by version.
