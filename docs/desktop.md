# Desktop Application

`SimpleWeather.Desktop` is the WPF desktop client in this solution.

## Purpose

The desktop application provides a Windows UI over the shared `SimpleWeather` library for interactive weather lookups.

It requests current conditions only, with a five-minute automatic refresh interval and a manual Refresh button. It uses automatic service selection (3.0, then 4.0, then free after access denial) and does not request hourly or daily forecasts. City lookups also require geocoding requests. See [request counts and polling](library.md#request-counts-and-polling).

## Framework

The desktop application targets:
- `.NET 10`
- WPF on Windows

## Configuration

The desktop app checks for an API key in this order:

1. `App.config`
2. `.local/openweather-api-key.txt`
3. `%AppData%\\SimpleWeather\\desktop-api-key.txt`

If the tracked `App.config` contains only a placeholder value, the app will fall back to the local secret file.

## Local State

The desktop application uses local persisted settings for convenience features such as:
- recently used locations
- last selected units
- saved window placement

These settings are machine-local and are not intended to be stored in the published repository.

## Running

Build the solution and run `SimpleWeather.Desktop` from Visual Studio or from the built output.

Before running, ensure a valid OpenWeather API key is available through one of the supported local configuration sources.
