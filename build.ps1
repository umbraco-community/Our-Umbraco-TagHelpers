# Packs Our.Umbraco.TagHelpers into ./build.out
# The version comes from <Version> in Our.Umbraco.TagHelpers.csproj; pass -Version to override it,
# e.g. pwsh ./build.ps1 -Version 2.1.0-rc1
param(
    [string]$Version
)

$projectFile = "./Our.Umbraco.TagHelpers/Our.Umbraco.TagHelpers.csproj"
$configuration = "Release"
$outputDirectory = "./build.out"

# Delete all files in the output directory
if (Test-Path $outputDirectory) {
    Remove-Item "$outputDirectory/*" -Recurse -Force
}

$packArgs = @($projectFile, "--configuration", $configuration, "--output", $outputDirectory)
if ($Version) {
    $packArgs += "/p:Version=$Version"
}

# Pack the project into a NuGet package
dotnet pack @packArgs

# Check if the pack was successful
if ($LASTEXITCODE -eq 0) {
    Write-Output "Pack successful."
} else {
    Write-Output "Pack failed."
    exit $LASTEXITCODE
}
