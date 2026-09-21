# Changelog

This changelog records shipped features, fixes, compatibility and runtime dependency changes. See [development and validation history](DEVELOPMENT-HISTORY.md) for tests, CI, build tooling and work not yet released.

## [2.0.0] - 2026-09-21

### Added

- One Call 4.0 current weather and paginated daily/hourly forecasts, preserving existing 3.0 and free-endpoint support.
- Per-controller selection of automatic, 3.0, 4.0 or free service using the same account key; explicit choices report access denial without changing services.
- Combined current-weather/forecast snapshots, using one request on 3.0 and allowing hourly data to be omitted to reduce 4.0 usage.

### Changed

- Remove the unused log4net dependency and the console sample's unused Newtonsoft dependencies.
- The console sample retrieves current conditions and daily forecasts together. The widget omits unused hourly forecast requests and uses the returned daily readings to determine its display length.
- Replace Newtonsoft.Json with attribute-mapped System.Text.Json response models. Remove public JToken constructors and the JToken coordinate helper; provide serializer-independent FromJson factories and GetCoordinatesFromJson.
- See the [2.0 migration guide](docs/migration-v2.md) for constructor replacements, exception types and deployment guidance.

See [release notes](docs/release-notes/v2.0.0.md) for subscription and request-count details.

## [1.0.3] - 2026-09-12

### Changed

- Update log4net to 3.4.0 and the .NET Framework Microsoft.Bcl.AsyncInterfaces and Microsoft.Bcl.Memory dependencies to 10.0.12.

### Fixed

- Use invariant coordinates and escaped query values in weather and geocoding requests.

- Parse geocoding city objects and integral JSON coordinates correctly.

- Dispose HTTP responses and consistently reject requests on a disposed controller.

- Parse numeric string readings independently of the current regional settings.

- Populate One Call current wind and sun times, and the free current-weather station metadata.

- Populate free forecast temperature, wind and cloud fields, calculate hourly wind direction after reading its bearing, and report precipitation probability as a percentage.

- Select representative daily weather nearest noon and group free forecast entries using the full time zone offset, including fractional hours.

Public constructor and method signatures are unchanged. These fixes correct previously incorrect returned values.

[1.0.3]: https://github.com/oznetmaster/SimpleWeatherClient/compare/v1.0.2...v1.0.3
[2.0.0]: https://github.com/oznetmaster/SimpleWeatherClient/compare/v1.0.3...v2.0.0
