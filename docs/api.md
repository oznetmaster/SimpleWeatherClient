# API Documentation

This repository contains two different kinds of documentation:

1. **Repo-facing Markdown docs** in `docs/` for contributors and GitHub readers
2. **Generated API reference** produced from XML comments and DocFX assets under `docfx/`

If you are looking for the actual API surface documentation for the `SimpleWeather` library, the generated API reference is the important part.

## API Reference Source

The generated API documentation is based on:
- XML documentation comments in the `SimpleWeather` library
- DocFX metadata generation from `SimpleWeather/SimpleWeather.csproj`
- conceptual DocFX content under `docfx/articles/`

## Current API Documentation Entry Points

Within the repository, the relevant DocFX entry points are:

- `docfx/toc.yml` - top-level DocFX table of contents
- `docfx/articles/overview.md` - DocFX conceptual overview page
- `docfx/api/toc.yml` - generated API reference table of contents

## What the API Docs Cover

The API reference is intended to document:
- `WeatherController`
- `GeoLocator`
- the weather model types
- utility types exposed by the library
- XML-comment-based method, type, and property descriptions

## Repository Docs vs API Docs

The files under `docs/` are for:
- overview
- configuration
- desktop app behavior
- widget app behavior
- release workflow guidance

Those files are not a replacement for the generated API reference.

## Recommended Public Publishing Model

The best public documentation setup for this repository is:

- `README.md` as the GitHub landing page
- `docs/*.md` for contributor and usage guidance
- DocFX published through GitHub Pages for the actual API reference site

Once GitHub Pages is wired up, this page should link directly to the published DocFX site URL.
