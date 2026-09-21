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

## Selecting a service

Service selection belongs to each controller, not to the API key. An account with access to all services can use the same key in independent clients:

```csharp
using var legacy = new WeatherController(apiKey, OpenWeatherService.OneCall3);
using var newer = new WeatherController(apiKey, OpenWeatherService.OneCall4);
using var free = new WeatherController(apiKey, OpenWeatherService.Free);
```

Explicit selections call only the selected service, without subscription probes against another version or fallback. A denied request throws `UnauthorizedAccessException` identifying the service; check both account subscription and key because a denial alone does not distinguish the cause. Rate limits and server failures produce `HttpRequestException`; transport/cancellation errors remain errors. Selecting a service does not subscribe the account to it. Geocoding remains a separate OpenWeather service.

The existing `new WeatherController(apiKey)` constructor selects `Automatic`: 3.0, then 4.0, then free, advancing only when access is denied. This preserves the single-call 3.0 route for existing subscribers. The `Service` property reports the controller's immutable selection, not the service most recently reached through automatic fallback.

## Combined current weather and forecast

Use the combined method when both current weather and a forecast are needed:

```csharp
using var weather = new WeatherController(apiKey);
WeatherSnapshot snapshot = await weather.GetWeatherSnapshotAsync(
    new LatLong(latitude, longitude), includeHourly: false, units: "metric");
CurrentWeather current = snapshot.CurrentWeather;
WeatherForecast forecast = snapshot.Forecast;
```

Existing separate current-weather and forecast methods remain available. The combined method prefers 3.0, then tries 4.0 if access is denied, then free endpoints if both deny access. It does not migrate accounts or change subscriptions. Disabling hourly data leaves `Forecast.Hourly` empty; daily forecast data remains available.

## Request counts and polling

| Operation | Successful 3.0 requests | Normal successful 4.0 requests |
| --- | ---: | ---: |
| Current weather | 1 | 1 |
| Forecast: 8 days and 48 hours | 1 | 4 |
| Combined current and full forecast | 1 | 5 |
| Combined current and daily forecast only | 1 | 2 |

In automatic mode, the 4.0 route also attempts one 3.0 request that is denied. Explicit 4.0 selection avoids that probe. Counts above describe weather requests, not billing guarantees: failed requests, geocoding, other applications and account terms must also be considered. The free fallback uses separate current and forecast requests after subscription probes; explicit free selection avoids those probes.

One Call 4.0 currently returns at most ten daily or twenty hourly readings per page. The client reads only enough pages for its existing eight-day/48-hour forecast contract, and does not follow the longer timeline unnecessarily. Unusually short pages may need additional requests. Provider continuation links are validated and always requested over HTTPS, including when the provider returns an HTTP link.

The library does not cache weather or schedule polling. Applications should reuse recent successful readings, throttle failed attempts as well as successful refreshes, avoid overlapping requests, and request only the data they display. At a ten-minute interval, a continuously running daily/current-only 4.0 consumer normally makes 288 successful weather requests per day, plus probes and any geocoding. This is per consumer; multiple applications or machines using the account add to the total. Configure account limits with OpenWeather rather than assuming one application's count represents the entire account.

This integration covers current weather and the existing daily/hourly forecast models. It does not add historical queries, longer-range timelines, minutely data or 4.0 alert-detail retrieval. See [OpenWeather's API documentation](https://openweathermap.org/api/one-call-4).

For a forecast without current conditions, use `GetWeatherForecastAsync(latitude, longitude, includeHourly: false)` or the city-name overload with `includeHourly: false`. One Call 4.0 then normally needs only the daily timeline request. Existing overloads continue to include hourly forecasts.

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
