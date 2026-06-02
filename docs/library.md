# SimpleWeather Library

The `SimpleWeather` project is the reusable API library in this solution.

## Purpose

The library provides a strongly typed wrapper over the OpenWeather APIs and exposes models and controllers for retrieving and working with weather data.

## Main Areas

The library includes:
- `WeatherController` for retrieving current weather and forecast data
- geolocation helpers such as `GeoLocator`
- strongly typed weather models
- parsing and utility support code

## Targeting Strategy

The library is dual-targeted:
- `net472`
- `net10.0`

This allows the same library surface to be consumed by both legacy and modern .NET applications.

## Language and Compatibility

The project uses the latest C# language version even for the `.NET Framework 4.7.2` target.

To support that model, the legacy target includes compatibility packages and shims where required so it can coexist with newer language/runtime-facing features used with the `.NET 10` target.

## Package Identity

For GitHub and NuGet publication, the package identity is:

- `SimpleWeatherClient`

This avoids collision with the existing `SimpleWeather` GitHub and NuGet identities.

## API Documentation

XML documentation is enabled for the library.

DocFX assets are included under `docfx/` so API documentation can be generated and published separately from the repo-facing Markdown docs.
