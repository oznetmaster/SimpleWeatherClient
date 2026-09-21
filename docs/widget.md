# Widget Application

`SimpleWeather.Widget` is the WinUI widget-style client in this solution.

## Purpose

The widget project provides a compact Windows UI experience built on top of the shared `SimpleWeather` library.

## Framework

The widget application targets:
- `.NET 10`
- Windows App SDK / WinUI

## Configuration and Local Settings

The widget uses local settings storage for widget-specific preferences and runtime state.

That includes values such as:
- location settings
- refresh interval
- layout/window state
- optional API key values stored for widget-specific use

The widget’s settings are local application data, not repository content.

## Weather service and request usage

The widget uses automatic service selection: One Call 3.0, then One Call 4.0, then the free endpoints, advancing only when access is denied. An API key with access to the chosen service is required. Current weather and the forecast window refresh independently.

The forecast window displays up to seven returned daily readings and requests no hourly pages. Its 30-minute timer refreshes only while visible; opening an expired forecast can also refresh it. The current-weather interval is configured separately in Settings. Geocoding and automatic service probes add requests; see [request counts and polling](library.md#request-counts-and-polling).

## Running

Build the solution and run `SimpleWeather.Widget` on a supported Windows environment.

Because the widget is built on modern Windows UI technology, it should be treated as the modern client in the solution, while the shared `SimpleWeather` library remains reusable across both legacy and modern consumers.
