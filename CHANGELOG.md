# Changelog

## CI validation - 2026-09-15 (no package release)

- Revalidate the current default-branch source after successful release workflows, including version commits created by GitHub Actions.
- Allow maintainers to configure exact-source, App-specific checks that must pass before publishing through `RELEASE_REQUIRED_CHECKS`; missing, failed or unconfirmed checks block the release.

## Test and desktop development updates - 2026-09-15 (no library release)

- Update the test SDK and Windows widget dependencies.
- Accept coordinates and an optional postcode/country in the console instead of embedding a developer's location. Invalid arguments exit before any weather request.
- The released library API and runtime code remain unchanged at 1.0.3.

## [1.0.3] - 2026-09-12

### Added

- NUnit tests for .NET Framework 4.7.2 and .NET 10, integrated into the existing Visual Studio solution.
- Offline regression coverage for weather HTTP behavior, geocoding and response models.
- Opt-in, read-only live OpenWeather tests for current conditions, forecasts and geocoding using private `LiveTestSettings.json` settings.
- Credential-free live/unit runsettings, a sample settings file, testing documentation, and CI/release test gates.

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

### Validation

- 117 offline tests passed on each of net472 and net10.0.
- Four opt-in live OpenWeather tests passed on each framework using an existing account.
- Private live settings are locally excluded from Git, never copied to output, and never packaged.

See [v1.0.3 release notes](docs/release-notes/v1.0.3.md).

[1.0.3]: https://github.com/oznetmaster/SimpleWeatherClient/compare/v1.0.2...v1.0.3
