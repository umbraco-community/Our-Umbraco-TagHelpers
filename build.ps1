# Define the project file and configuration
$projectFile = "./Our.Umbraco.TagHelpers/Our.Umbraco.TagHelpers.csproj"
$configuration = "Release"
$outputDirectory = "./build.out"
$version = "2.0.0"

# Delete all files in the output directory
if (Test-Path $outputDirectory) {
    Remove-Item "$outputDirectory/*"
}

# Pack the project into a NuGet package
dotnet pack $projectFile --configuration $configuration --output $outputDirectory /p:Version=$version

# Check if the pack was successful
if ($LASTEXITCODE -eq 0) {
    Write-Output "Pack successful."
} else {
    Write-Output "Pack failed."
    exit $LASTEXITCODE
}