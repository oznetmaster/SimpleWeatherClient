# Releases and Packages

This repository is set up for GitHub-based release publishing and NuGet package publication.

## Package Name

The library package is published as:

- `SimpleWeatherClient`

This avoids conflict with the existing `SimpleWeather` package identity.

## Release Workflow

The GitHub Actions release workflow:
- verifies that `docs/release-notes/v<version>.md` exists
- restores and builds the library and NUnit test project
- runs offline tests for net472 and net10.0 before packaging
- packs the `SimpleWeather` library project
- publishes the `.nupkg` package to NuGet
- publishes the `.snupkg` symbol package to NuGet
- creates the GitHub release using the reviewed release notes and attaches both packages

## Version Source

Package versioning is derived from:
- a published GitHub release tag, or
- a manually supplied version during workflow dispatch

Accepted examples include:
- `v1.0.0`
- `v1.2.3`
- `v1.0.0-preview.1`

The workflow strips a leading `v` before assigning the NuGet package version.

## GitHub Secret Required

Repository Actions must include this secret:

- `NUGET_API_KEY`

This secret should contain the NuGet.org API key used for package publishing.

## Public Package Considerations

For public usage, the Release configuration now generates portable PDB files so the symbol package contains valid symbols and can be published successfully.

## Suggested Release Flow

1. Update `CHANGELOG.md` with a dated version entry and add `docs/release-notes/v<version>.md`.
2. Validate the tests and ensure private live settings are locally excluded and absent from release files.
3. Commit and push the release changes to `main`.
4. Create and push the matching tag, such as `v1.0.3`.
5. Let GitHub Actions validate, build, test, package, publish to NuGet, and create the GitHub release.

See the [changelog](../CHANGELOG.md) and [v1.0.3 release notes](release-notes/v1.0.3.md).

## Manual Testing

A manual workflow dispatch is useful for verifying pack/version behavior.

A prerelease version such as `0.1.0-preview.1` is a good choice for private validation before the first stable public release.
