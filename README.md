# SimpleWeatherClient

For shipped changes, see the [changelog](CHANGELOG.md). Test, CI and build history is recorded separately in [development and validation history](DEVELOPMENT-HISTORY.md).


SimpleWeatherClient is a Windows solution built around the `SimpleWeather` library for working with the OpenWeather APIs.

This repository contains:
- the `SimpleWeather` reusable API library
- a WPF desktop client
- a WinUI widget client
- a small console-based test harness
- an NUnit test suite with offline tests and opt-in live OpenWeather checks

> Website: https://openweathermap.org/

## Original Project and Attribution

This solution is adapted from the original [`Banovvv/SimpleWeather`](https://github.com/Banovvv/SimpleWeather) project by **Ivan Gechev** and continues to respect the original MIT licensing and attribution requirements.

The original project was a .NET 6 weather library. This repository extends that foundation into a broader solution with additional applications, updated targeting, local configuration improvements, and ongoing modernization work.

## What Is Different in This Adaptation

Compared with the original upstream project, this repository currently differs in several important ways:

- **Broader solution structure**
  - The repo is no longer just a single library project.
  - It now includes multiple applications built around the shared `SimpleWeather` library.

- **Dual-targeted library**
  - `SimpleWeather` targets both:
    - `.NET Framework 4.7.2`
    - `.NET 10`
  - This allows the API library to be used from both legacy and modern .NET applications.
  - The `.NET Framework 4.7.2` target still uses the latest C# language version.
  - Compatibility packages and shims are included so the legacy target can support newer language/runtime-facing features used alongside the `.NET 10` target.

- **Additional Windows clients**
  - `SimpleWeather.Desktop` is a WPF desktop application.
  - `SimpleWeather.Widget` is a WinUI-based widget-style client.

- **Safer local API-key handling**
  - Real API keys are not intended to be stored in tracked repository files.
  - The apps can fall back to a local non-published key file for local development and testing.

- **Expanded documentation groundwork**
  - XML documentation is enabled for the `SimpleWeather` library.
  - DocFX assets are included for generating API documentation.

- **Solution-level modernization work**
  - Nullable reference types and newer C# features are in use in the modernized code.
  - The solution is being prepared for cleaner GitHub publication and NuGet packaging workflows.

## Solution Structure

- `SimpleWeather/` - shared weather API library
- `SimpleWeather.Desktop/` - WPF desktop app
- `SimpleWeather.Widget/` - WinUI widget client
- `SimpleWeatherTest/` - console test application
- `SimpleWeather.Tests/` - NUnit tests for .NET Framework 4.7.2 and .NET 10
- `docs/` - repo-facing markdown documentation
- `docfx/` - API documentation assets and generated content

## Features

### `SimpleWeather` library
- strongly typed weather models
- current weather retrieval
- forecast retrieval
- geolocation helpers
- support for OpenWeather-based weather queries from reusable .NET code

### Desktop and widget clients
- weather lookup from Windows UI applications
- shared use of the `SimpleWeather` library
- local, non-published API-key fallback support for development use

## Documentation

If you are looking for the actual API surface documentation for the `SimpleWeather` library, start here:

- [Published API Documentation Site](https://oznetmaster.github.io/SimpleWeatherClient/)
- [API Documentation Guide](docs/api.md)

Additional repository and contributor documentation:

- [Overview](docs/overview.md)
- [Configuration](docs/configuration.md)
- [Library](docs/library.md)
- [Desktop Application](docs/desktop.md)
- [Widget Application](docs/widget.md)
- [Releases and Packages](docs/releases.md)
- [Automated and Live Tests](docs/testing.md)
- [Changelog](CHANGELOG.md)
- [v1.0.3 Release Notes](docs/release-notes/v1.0.3.md)

The repository docs in `docs/` are for contributors and GitHub readers.
The generated API/reference documentation is built from XML comments and DocFX assets under `docfx/` and published to GitHub Pages.

## Requirements

To build and run the full solution on Windows, you will typically want:
- Visual Studio 2026 or later with .NET desktop development tools
- .NET 10 SDK
- .NET Framework 4.7.2 targeting pack / developer tools
- Windows 10/11 for the desktop and widget clients

## Local Configuration

This repository is set up so a tracked `App.config` can contain only placeholders while local development still works.

### Free account and API key

A valid **OpenWeather API key is required**, including for the free service. There is no separate SimpleWeatherClient key. See [OpenWeather's API key guidance](https://docs.openweather.co.uk/faq).

The library prefers **One Call API 3.0** for accounts that already have access. When that service denies access, it tries **One Call API 4.0**, then OpenWeather's free current-weather and five-day forecast endpoints if 4.0 also denies access. The same account key is used throughout; each service still requires the appropriate account access. Timeouts, rate limits and server errors do not trigger this fallback. Free endpoints provide less information and shorter forecast coverage; fallback does not provide anonymous access or make an invalid key usable.

Existing 3.0 customers do not need to migrate. OpenWeather states that 3.0 remains available and recommends 4.0 for new integrations; 4.0 has a separate subscription. See its [migration guide](https://openweathermap.org/api/one-call-3-migration). Support for 4.0 and the combined snapshot API starts in version 2.0.0; older releases support 3.0 and free endpoints only.

**Request costs differ.** `GetWeatherSnapshotAsync` retrieves current conditions and forecasts together: one successful 3.0 request, or normally five 4.0 requests for current weather, eight daily forecasts and 48 hourly forecasts. Set `includeHourly: false` when hourly data is unused: this normally reduces 4.0 to two requests. A denied 3.0 probe precedes the 4.0 route; geocoding is separate. See [request counts and polling guidance](docs/library.md#request-counts-and-polling). The library does not impose a polling interval or account-wide quota.

You can instead select `OpenWeatherService.OneCall3`, `OneCall4`, or `Free` in the new constructor overload. Each controller uses its own selection; all may share the same account key. Explicit selection bypasses other services completely and reports access denial rather than silently falling back. [Service-selection examples](docs/library.md#selecting-a-service) explain this and the request-count differences.

### API key lookup order

The desktop app and test app check for an OpenWeather API key in this order:

1. `App.config`
2. solution-local secret file: `.local/openweather-api-key.txt`
3. local AppData fallback: `%AppData%\SimpleWeather\desktop-api-key.txt`

### Recommended local setup

For portable local development, create this file in the solution root:

`.local/openweather-api-key.txt`

Its contents should be only the raw API key, for example:

```text
YOUR_OPENWEATHER_API_KEY_HERE
```

That `.local` folder is intended to remain local and non-published.

## Building the Solution

From the repository root:

```powershell
dotnet build .\SimpleWeather.sln
```

Or build from Visual Studio.

## Running the Projects

- `SimpleWeather.Desktop` - desktop UI client
- `SimpleWeather.Widget` - widget-style Windows client
- `SimpleWeatherTest` - console-based smoke test / development harness

Before running applications that call the OpenWeather service, make sure a valid API key is available through one of the supported local configuration paths.

Supply the console's location as arguments, using decimal points for coordinates:

```powershell
dotnet run --project .\SimpleWeatherTest -- 51.5074 -0.1278
```

An optional postcode and country code add a postcode lookup, for example `51.5074 -0.1278 "SW1A 1AA" GB`. Missing or invalid coordinates display usage and exit before contacting OpenWeather. No personal location is embedded in the console sample.

## Automated Tests

Run the offline NUnit suite without OpenWeather credentials:

```powershell
dotnet test .\SimpleWeather.Tests\SimpleWeather.Tests.csproj --settings .\SimpleWeather.Tests\unit.runsettings
```

The separate **Live** fixture can use your OpenWeather account and current data. It requires private `LiveTestSettings.json` settings and an explicit enable flag. See [testing instructions](docs/testing.md) for Visual Studio, command-line use, local exclusions, and settings supplied by external runners.

## Publishing Workflow

This repository is set up for:
- GitHub publication under the `SimpleWeatherClient` name
- automated NuGet package creation
- release-based package publishing

## License

This repository includes MIT-licensed upstream work and preserves attribution to the original author where required.

See:
- `LICENSE`
- `SimpleWeather/LICENSE`
- project source headers in adapted files

## Acknowledgments

- Original upstream project: [`Banovvv/SimpleWeather`](https://github.com/Banovvv/SimpleWeather)
- Original author: **Ivan Gechev**
- Current adaptation and expansion: **Neil Colvin**

## Publishing when local hardware is unavailable

The publish/release workflows support an explicit manual override when the processor or local self-hosted GitHub Actions runner is unavailable. Select `skip_hardware_checks` and provide a single-line `hardware_skip_reason`. Use the workflow's normal source and version controls. The override applies only to that invocation and is recorded with the exact source revision in its warning and job summary; it does not create a passing hardware-test result.

GitHub-hosted validation remains mandatory for the checked-out source, and the normal build, tests and packaging steps still run. Wait for the configured hosted workflows to pass, or run them on the same source revision first. None of these hosted checks needs the local runner or processor. Automatic tag/release-triggered runs retain the normal hardware checks; use a manual invocation of the updated release workflow when an offline override is needed.

## Upgrading to 2.0

The package name and namespace are unchanged. See the [migration guide](docs/migration-v2.md) for the removal of public Newtonsoft types, serializer-independent model factories and net472 deployment requirements.
