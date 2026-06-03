[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$Token,

    [Parameter(Mandatory = $true)]
    [string]$Owner,

    [ValidateSet("organization", "user")]
    [string]$Scope = "organization",

    [ValidateSet("nuget", "npm", "maven", "rubygems", "docker", "container")]
    [string]$PackageType = "nuget",

    [string]$Prefix = "Pottmayer.Tars.",
    [string[]]$IncludePackages = @(),
    [string[]]$ExcludePackages = @(),
    [string[]]$IncludeVersions = @(),
    [switch]$DeleteVersions,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$apiBaseUrl = "https://api.github.com"
$apiVersion = "2022-11-28"
$pageSize = 100

function Get-AuthHeaders {
    return @{
        Accept = "application/vnd.github+json"
        Authorization = "Bearer $Token"
        "X-GitHub-Api-Version" = $apiVersion
        "User-Agent" = "pottmayer-tars-delete-github-packages"
    }
}

function Get-ScopePath {
    param(
        [string]$ResourceSuffix
    )

    if ($Scope -eq "organization") {
        return "/orgs/$Owner$ResourceSuffix"
    }

    return "/user$ResourceSuffix"
}

function Invoke-GitHubGet {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return Invoke-RestMethod -Method Get -Uri "$apiBaseUrl$Path" -Headers (Get-AuthHeaders)
}

function Invoke-GitHubDelete {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    # Invoke-RestMethod (not Invoke-WebRequest) so DELETE never blocks on an
    # interactive prompt under -NonInteractive.
    Invoke-RestMethod -Method Delete -Uri "$apiBaseUrl$Path" -Headers (Get-AuthHeaders) | Out-Null
}

function Get-AllPackages {
    $all = @()
    $page = 1

    do {
        $path = Get-ScopePath "/packages?package_type=$PackageType&per_page=$pageSize&page=$page"
        # Assign to a variable first: Invoke-RestMethod emits a JSON array as a
        # single object, so @($response) flattens it but @(Invoke-GitHubGet ...)
        # would keep the whole page nested as one element.
        $response = Invoke-GitHubGet -Path $path
        $currentPage = @($response)
        $all += $currentPage
        $page++
    } while ($currentPage.Count -eq $pageSize)

    return $all
}

function Get-PackageVersions {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageName
    )

    $encodedPackageName = [Uri]::EscapeDataString($PackageName)
    $versions = @()
    $page = 1

    do {
        $path = Get-ScopePath "/packages/$PackageType/$encodedPackageName/versions?per_page=$pageSize&page=$page"
        $response = Invoke-GitHubGet -Path $path
        $currentPage = @($response)
        $versions += $currentPage
        $page++
    } while ($currentPage.Count -eq $pageSize)

    return $versions
}

function Test-PackageSelected {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if ($Prefix -ne "" -and $Name -notlike "$Prefix*") {
        return $false
    }

    if ($IncludePackages.Count -gt 0 -and $IncludePackages -notcontains $Name) {
        return $false
    }

    if ($ExcludePackages.Count -gt 0 -and $ExcludePackages -contains $Name) {
        return $false
    }

    return $true
}

function Test-VersionIncluded {
    param(
        [Parameter(Mandatory = $true)]
        $Version
    )

    if ($IncludeVersions.Count -eq 0) {
        return $true
    }

    $versionLabel = [string]$Version.name
    if ([string]::IsNullOrWhiteSpace($versionLabel)) {
        return $false
    }

    return $IncludeVersions -contains $versionLabel
}

function Remove-Package {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageName
    )

    $encodedPackageName = [Uri]::EscapeDataString($PackageName)
    Invoke-GitHubDelete -Path (Get-ScopePath "/packages/$PackageType/$encodedPackageName")
}

function Remove-PackageVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageName,

        [Parameter(Mandatory = $true)]
        [long]$PackageVersionId
    )

    $encodedPackageName = [Uri]::EscapeDataString($PackageName)
    Invoke-GitHubDelete -Path (Get-ScopePath "/packages/$PackageType/$encodedPackageName/versions/$PackageVersionId")
}

Write-Host "Listing '$PackageType' packages from GitHub Packages ($($Scope): $Owner)..." -ForegroundColor Cyan

# Materialize the full list first, then filter explicitly. Filtering a streamed
# function pipeline directly proved unreliable here.
$allPackages = @(Get-AllPackages)

$packages = @(
    $allPackages |
        Where-Object { Test-PackageSelected -Name ([string]$_.name) } |
        Sort-Object name
)

if ($packages.Count -eq 0) {
    Write-Host "No matching packages found." -ForegroundColor Yellow
    exit 0
}

if ($Prefix -ne "") {
    Write-Host "Filter: package name starts with '$Prefix'" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "Packages selected ($($packages.Count)):" -ForegroundColor Green
$packages | ForEach-Object {
    Write-Host " - $($_.name)"
}

if (-not $Force) {
    Write-Host ""
    Write-Warning "This operation deletes packages from GitHub Packages."
    Write-Warning "Re-run with -Force to proceed, or add -WhatIf to preview the exact deletions."
    exit 1
}

foreach ($package in $packages) {
    $packageName = [string]$package.name

    if ($DeleteVersions) {
        $versions = Get-PackageVersions -PackageName $packageName |
            Where-Object { Test-VersionIncluded -Version $_ }

        if (-not $versions) {
            Write-Host ""
            if ($IncludeVersions.Count -gt 0) {
                Write-Host "No matching versions found for $packageName" -ForegroundColor Yellow
            }
            else {
                Write-Host "No versions found for $packageName" -ForegroundColor Yellow
            }
            continue
        }

        foreach ($version in $versions) {
            $versionLabel = if ($version.name) { $version.name } else { $version.id }
            $target = "$packageName version $versionLabel"

            if ($PSCmdlet.ShouldProcess($target, "Delete package version")) {
                Write-Host ""
                Write-Host "Deleting $target" -ForegroundColor Cyan
                Remove-PackageVersion -PackageName $packageName -PackageVersionId ([long]$version.id)
            }
        }

        continue
    }

    if ($PSCmdlet.ShouldProcess($packageName, "Delete package")) {
        Write-Host ""
        Write-Host "Deleting package $packageName" -ForegroundColor Cyan
        Remove-Package -PackageName $packageName
    }
}

Write-Host ""
Write-Host "GitHub Packages deletion flow completed." -ForegroundColor Green
