# Changelog

This changelog records shipped features, fixes, compatibility and runtime dependency changes. See [development and validation history](DEVELOPMENT-HISTORY.md) for tests, CI, build tooling and work not yet released.

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