param(
    [Parameter(Mandatory = $true)]
    [string]$Owner,

    [Parameter(Mandatory = $true)]
    [string]$Username,

    [Parameter(Mandatory = $true)]
    [string]$Token,

    [string]$Configuration = "Release",
    [string]$Version = "0.0.1",
    [string]$OutputDirectory = ".artifacts/github-packages",
    [string]$SourceName = "github",
    [switch]$PackOnly,
    [switch]$SkipDuplicate,
    [string[]]$ExcludePackages = @(),
    [string[]]$IncludePackages = @()
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "Pottmayer.Tars.slnx"
$absoluteOutputDirectory = Join-Path $repoRoot $OutputDirectory
$sourceUrl = "https://nuget.pkg.github.com/$Owner/index.json"

if (-not (Test-Path $solutionPath)) {
    throw "Solution file not found: $solutionPath"
}

New-Item -ItemType Directory -Force -Path $absoluteOutputDirectory | Out-Null

Write-Host "Packing projects into $absoluteOutputDirectory" -ForegroundColor Cyan

$packArgs = @(
    "pack",
    $solutionPath,
    "-c", $Configuration,
    "-o", $absoluteOutputDirectory,
    "/p:Version=$Version",
    "/p:PackageVersion=$Version"
)

& dotnet @packArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet pack failed."
}

$packages = Get-ChildItem -Path $absoluteOutputDirectory -Filter "*.nupkg" |
    Where-Object { $_.Name -notlike "*.symbols.nupkg" } |
    Sort-Object Name

if ($IncludePackages.Count -gt 0) {
    $packages = $packages | Where-Object {
        $packageId = $_.BaseName -replace '\.\d+\.\d+\.\d+.*$', ''
        $IncludePackages -contains $packageId
    }
}

if ($ExcludePackages.Count -gt 0) {
    $packages = $packages | Where-Object {
        $packageId = $_.BaseName -replace '\.\d+\.\d+\.\d+.*$', ''
        $ExcludePackages -notcontains $packageId
    }
}

if (-not $packages) {
    throw "No packages found to publish in $absoluteOutputDirectory."
}

Write-Host "" 
Write-Host "Packages ready:" -ForegroundColor Green
$packages | ForEach-Object { Write-Host " - $($_.Name)" }

if ($PackOnly) {
    Write-Host ""
    Write-Host "PackOnly enabled. No packages were pushed." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Configuring source '$SourceName' => $sourceUrl" -ForegroundColor Cyan

$removeSourceArgs = @("nuget", "remove", "source", $SourceName)
& dotnet @removeSourceArgs 2>$null | Out-Null

$addSourceArgs = @(
    "nuget", "add", "source", $sourceUrl,
    "--name", $SourceName,
    "--username", $Username,
    "--password", $Token,
    "--store-password-in-clear-text"
)

& dotnet @addSourceArgs
if ($LASTEXITCODE -ne 0) {
    throw "Failed to configure NuGet source '$SourceName'."
}

foreach ($package in $packages) {
    Write-Host ""
    Write-Host "Publishing $($package.Name)" -ForegroundColor Cyan

    $pushArgs = @(
        "nuget", "push", $package.FullName,
        "--source", $SourceName,
        "--api-key", $Token
    )

    if ($SkipDuplicate) {
        $pushArgs += "--skip-duplicate"
    }

    & dotnet @pushArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to publish package: $($package.Name)"
    }
}

Write-Host ""
Write-Host "All packages published successfully to $sourceUrl" -ForegroundColor Green
