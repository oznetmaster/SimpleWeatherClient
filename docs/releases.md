# Releases and Packages

This repository is set up for GitHub-based release publishing and NuGet package publication.

## Package Name

The library package is published as:

- `SimpleWeatherClient`

This avoids conflict with the existing `SimpleWeather` package identity.

## Release Workflow

The GitHub Actions release workflow:
- restores and builds the solution
- packs the `SimpleWeather` library project
- publishes the `.nupkg` package to NuGet
- publishes the `.snupkg` symbol package to NuGet

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

1. Ensure the package and workflow changes are pushed to `main`
2. Create a GitHub release with a tag such as `v1.0.0`
3. Publish the release
4. Let GitHub Actions build, pack, and publish the package to NuGet

## Manual Testing

A manual workflow dispatch is useful for verifying pack/version behavior.

A prerelease version such as `0.1.0-preview.1` is a good choice for private validation before the first stable public release.
