[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
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
$apiVersion = "2026-03-10"
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

function Invoke-GitHubJson {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("GET", "DELETE")]
        [string]$Method,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $uri = "$apiBaseUrl$Path"
    $headers = Get-AuthHeaders

    if ($Method -eq "GET") {
        return Invoke-RestMethod -Method Get -Uri $uri -Headers $headers
    }

    Invoke-WebRequest -Method Delete -Uri $uri -Headers $headers | Out-Null
}

function Get-AllPackages {
    $packages = @()
    $page = 1

    do {
        $path = Get-ScopePath "/packages?package_type=$PackageType&per_page=$pageSize&page=$page"
        $currentPage = @(Invoke-GitHubJson -Method GET -Path $path)
        $packages += $currentPage
        $page++
    } while ($currentPage.Count -eq $pageSize)

    return $packages
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
        $currentPage = @(Invoke-GitHubJson -Method GET -Path $path)
        $versions += $currentPage
        $page++
    } while ($currentPage.Count -eq $pageSize)

    return $versions
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
    $path = Get-ScopePath "/packages/$PackageType/$encodedPackageName"

    Invoke-GitHubJson -Method DELETE -Path $path
}

function Remove-PackageVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageName,

        [Parameter(Mandatory = $true)]
        [long]$PackageVersionId
    )

    $encodedPackageName = [Uri]::EscapeDataString($PackageName)
    $path = Get-ScopePath "/packages/$PackageType/$encodedPackageName/versions/$PackageVersionId"

    Invoke-GitHubJson -Method DELETE -Path $path
}

function Test-ShouldProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Target,

        [Parameter(Mandatory = $true)]
        [string]$Action
    )

    if ($null -eq $PSCmdlet) {
        return $true
    }

    return $PSCmdlet.ShouldProcess($Target, $Action)
}

Write-Host "Listing '$PackageType' packages from GitHub Packages ($($Scope): $Owner)..." -ForegroundColor Cyan

$packages = Get-AllPackages |
    Where-Object {
        ($Prefix -eq "" -or $_.name -like "$Prefix*") -and
        ($IncludePackages.Count -eq 0 -or $IncludePackages -contains $_.name) -and
        ($ExcludePackages.Count -eq 0 -or $ExcludePackages -notcontains $_.name)
    } |
    Sort-Object name

if (-not $packages) {
    Write-Host "No matching packages found." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Packages selected:" -ForegroundColor Green
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

            if (Test-ShouldProcess -Target $target -Action "Delete package version") {
                Write-Host ""
                Write-Host "Deleting $target" -ForegroundColor Cyan
                Remove-PackageVersion -PackageName $packageName -PackageVersionId ([long]$version.id)
            }
        }

        continue
    }

    if (Test-ShouldProcess -Target $packageName -Action "Delete package") {
        Write-Host ""
        Write-Host "Deleting package $packageName" -ForegroundColor Cyan
        Remove-Package -PackageName $packageName
    }
}

Write-Host ""
Write-Host "GitHub Packages deletion flow completed." -ForegroundColor Green
