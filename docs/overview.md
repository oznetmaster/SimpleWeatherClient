# Overview

SimpleWeatherClient is a Windows-focused solution built around the `SimpleWeather` library for working with the [OpenWeather](https://openweathermap.org/) APIs.

If you are looking for the actual API surface documentation for the library, start with [API Documentation](api.md).

This repository contains:
- `SimpleWeather` - the reusable API library
- `SimpleWeather.Desktop` - a WPF desktop client
- `SimpleWeather.Widget` - a WinUI widget-style client
- `SimpleWeatherTest` - a console-based test harness

## Relationship to the Original Project

This solution is adapted from the original [`Banovvv/SimpleWeather`](https://github.com/Banovvv/SimpleWeather) repository by **Ivan Gechev**.

The upstream project began as a .NET 6 library. This adaptation expands that into a broader multi-project solution while preserving attribution and MIT licensing requirements.

## What Is Different in This Adaptation

Key differences from the original upstream library include:

- a broader solution structure with multiple applications
- a dual-targeted reusable library
- modernized project configuration and documentation support
- safer local API-key handling for development and testing
- release automation for GitHub and NuGet publication

## Target Frameworks

The `SimpleWeather` library targets:
- `.NET Framework 4.7.2`
- `.NET 10`

Although the legacy target remains on `.NET Framework 4.7.2`, it still uses the latest C# language version and relies on compatibility packages and shims where needed to align with newer language and runtime-facing features used alongside the `.NET 10` target.

## Repository Layout

- `SimpleWeather/` - shared library
- `SimpleWeather.Desktop/` - WPF desktop application
- `SimpleWeather.Widget/` - WinUI widget application
- `SimpleWeatherTest/` - console test application
- `docs/` - repo-facing markdown documentation
- `docfx/` - API documentation assets and generated content

## Documentation Model

This repository uses two documentation layers:

1. **Markdown docs in `docs/`** for GitHub readers and contributors
2. **DocFX content in `docfx/`** for generated API/reference material

The Markdown docs provide project guidance, while the generated API documentation remains the authoritative reference for the `SimpleWeather` library surface.
