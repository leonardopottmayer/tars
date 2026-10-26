# Publishing Packages

The packages are published to the GitHub Packages NuGet feed by the release workflow ([`.github/workflows/release.yml`](../../.github/workflows/release.yml)), never from a developer machine.

## How a release publishes

1. Changes reach `dev` through pull requests (squash-merged, Conventional Commit titles).
2. A pull request from `dev` to `main` gets a release preview comment with the next version. Merging it (merge commit) is the release.
3. The release bot (semantic-release, as a GitHub App) computes the version from the commits since the last tag, writes it to `<Version>` in [`Directory.Build.props`](../../Directory.Build.props) and to `CHANGELOG.md`, commits to `main`, tags `v<version>` and creates the GitHub Release.
4. The `publish` job checks out that tag, runs `dotnet pack` and pushes every package to `https://nuget.pkg.github.com/leonardopottmayer/index.json` with `GITHUB_TOKEN`.
5. The bot opens a `main` → `dev` pull request with the version bump, merged automatically once CI passes.

All packages share the one `<Version>` (lockstep). Only `feat`, `fix`, `perf` and breaking changes release; a merge with only `docs`, `ci`, `build`, `test`, `refactor` or `chore` commits releases nothing. Never edit `<Version>`, `CHANGELOG.md` or tags by hand.

## Publishing a release again

If the release was created but publishing failed, fix the cause and run the workflow by hand with the version (without the `v`):

```bash
gh workflow run release.yml -f version=0.0.19
```

Versions already on the feed are skipped.

## Testing a package before releasing

Pack into a local feed with a pre-release version and point the consuming project at it, instead of publishing:

```bash
dotnet pack -c Release -o ~/local-feed -p:PackageVersion=0.0.19-local.$(date +%Y%m%d%H%M%S)
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

- The deletion script uses the GitHub Packages REST API for package metadata and deletion:
  https://docs.github.com/en/enterprise-cloud@latest/rest/packages/packages
```
