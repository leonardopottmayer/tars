# Publishing Packages

This repository can publish its NuGet packages to a private GitHub Packages feed.

All packages share the version defined in [`Directory.Build.props`](../../Directory.Build.props) (currently `0.0.1`). The publish scripts can override it per run with `-Version`.

## Requirements

- A GitHub repository under your personal account or organization
- A GitHub Personal Access Token with:
  - `write:packages` to publish
  - `read:packages` to consume
  - `repo` if the repository is private

## Feed URL

Replace `OWNER_OR_ORG` with your GitHub username or organization:

```text
https://nuget.pkg.github.com/OWNER_OR_ORG/index.json
```

## Pack And Publish All Packages

From the repository root:

```powershell
.\scripts\publish-github-packages.ps1 `
  -Owner "OWNER_OR_ORG" `
  -Username "GITHUB_USERNAME" `
  -Token "GITHUB_PAT" `
  -Version "0.0.1" `
  -SkipDuplicate
```

## Pack Only

```powershell
.\scripts\publish-github-packages.ps1 `
  -Owner "OWNER_OR_ORG" `
  -Username "GITHUB_USERNAME" `
  -Token "GITHUB_PAT" `
  -Version "0.0.1" `
  -PackOnly
```

Packages are generated into:

```text
.artifacts/github-packages
```

## Publish Only A Few Packages

```powershell
.\scripts\publish-github-packages.ps1 `
  -Owner "OWNER_OR_ORG" `
  -Username "GITHUB_USERNAME" `
  -Token "GITHUB_PAT" `
  -Version "0.0.1" `
  -IncludePackages "Pottmayer.Tars.Core.Primitives","Pottmayer.Tars.Core.Cqrs"
```

## Exclude Packages

Use this to skip any package you do not want to publish in a given run:

```powershell
.\scripts\publish-github-packages.ps1 `
  -Owner "OWNER_OR_ORG" `
  -Username "GITHUB_USERNAME" `
  -Token "GITHUB_PAT" `
  -Version "0.0.1" `
  -ExcludePackages "Pottmayer.Tars.Web.Http.AspNetCore"
```

## Delete Published Packages

This repository also includes a deletion script for GitHub Packages.

Requirements:

- a Personal Access Token with `read:packages` and `delete:packages`
- organization admin permissions if the packages are owned by an organization

Preview what would be deleted:

```powershell
.\scripts\delete-github-packages.ps1 `
  -Owner "OWNER_OR_ORG" `
  -Token "GITHUB_PAT" `
  -Scope organization `
  -WhatIf `
  -Force
```

Delete all matching `Pottmayer.Tars.*` NuGet packages from an organization:

```powershell
.\scripts\delete-github-packages.ps1 `
  -Owner "OWNER_OR_ORG" `
  -Token "GITHUB_PAT" `
  -Scope organization `
  -Force
```

Delete only a few packages:

```powershell
.\scripts\delete-github-packages.ps1 `
  -Owner "OWNER_OR_ORG" `
  -Token "GITHUB_PAT" `
  -Scope organization `
  -IncludePackages "Pottmayer.Tars.Core.Primitives","Pottmayer.Tars.Core.Cqrs" `
  -Force
```

Delete versions one by one instead of deleting the whole package entry:

```powershell
.\scripts\delete-github-packages.ps1 `
  -Owner "OWNER_OR_ORG" `
  -Token "GITHUB_PAT" `
  -Scope organization `
  -DeleteVersions `
  -Force
```

Delete only specific versions, for example all `0.0.1` versions of matching packages:

```powershell
.\scripts\delete-github-packages.ps1 `
  -Owner "OWNER_OR_ORG" `
  -Token "GITHUB_PAT" `
  -Scope organization `
  -DeleteVersions `
  -IncludeVersions "0.0.1" `
  -Force
```

## Consume The Private Feed

You can configure the source directly:

```powershell
dotnet nuget add source "https://nuget.pkg.github.com/OWNER_OR_ORG/index.json" `
  --name github `
  --username "GITHUB_USERNAME" `
  --password "GITHUB_PAT" `
  --store-password-in-clear-text
```

Or copy `nuget.config.example` to `nuget.config`, replace the placeholders, and keep it local.

Then install packages normally:

```powershell
dotnet add package Pottmayer.Tars.Core.Primitives --source github
```

## Notes

- The script packs the solution and then pushes each generated package.
- The current solution includes every project tracked in [`Pottmayer.Tars.slnx`](../../Pottmayer.Tars.slnx) (at the repository root).
- Use `-ExcludePackages` to skip any package you do not want to publish.
- The deletion script uses the GitHub Packages REST API for package metadata and deletion:
  https://docs.github.com/en/enterprise-cloud@latest/rest/packages/packages
```
