# Migrating to SimpleWeatherClient 2.0

The repository, NuGet package name, assembly name and `SimpleWeather` namespace are unchanged. Update the `SimpleWeatherClient` package reference to 2.0.0 and rebuild consumers. Both net472 and .NET 10 remain supported.

Applications calling `WeatherController` or `GeoLocator` and reading the returned weather models normally require no source changes. Existing weather-query overloads and result properties remain available. The new service-selection and combined-snapshot methods are optional.

## JSON parsing is an implementation detail

The library now deserializes attribute-mapped response classes with `System.Text.Json`. It no longer depends on Newtonsoft.Json or exposes a serializer's DOM types. Optional readings remain nullable; unknown provider fields are ignored. The existing `GetCitiesByNameAsync` method still returns complete city JSON strings, including fields that the library does not otherwise interpret.

The public constructors accepting Newtonsoft's `JToken` have been removed. If you used one directly, use the corresponding `FromJson` factory with the same JSON fragment:

```csharp
// Before (requires Newtonsoft.Json):
var metrics = new Main(JObject.Parse(json));

// After (no serializer dependency):
var metrics = Main.FromJson(json);
```

If your application already has a JToken for another reason, `Main.FromJson(token.ToString())` is a transitional replacement. Your own Newtonsoft reference remains necessary until you remove that application usage.

Factories are available on `Alerts`, `Clouds`, `Coordinates`, `Current`, `Daily`, `FeelsLike`, `Hourly`, `Main`, `Rain`, `Snow`, `Sys`, `Temperature`, `Weather` and `Wind`. They accept each type's original JSON fragment; `Weather.FromJson` accepts the `weather` **array**, and `Clouds.FromJson` accepts either a number or the `{ "all": ... }` object. Passing null produces an empty model, as the former nullable-token constructors did.

The two full-response constructors are unchanged:

```csharp
var current = new CurrentWeather(currentResponseJson);
var forecast = new WeatherForecast(forecastResponseJson);
```

Replace `GeoUtils.GetCoordinatesFromJToken(token)` with `GeoUtils.GetCoordinatesFromJson(json)`. Pass an object with `lat` and `lon` properties.

## Exceptions and deployment

Code catching `Newtonsoft.Json.JsonException` for malformed provider JSON should catch `System.Text.Json.JsonException` instead. One Call 4.0 response-schema failures are reported as `InvalidDataException`; HTTP, cancellation and subscription-access failures retain their existing categories.

Modern .NET uses its built-in System.Text.Json. The net472 package references System.Text.Json and its supporting dependencies. Rebuild and redeploy merged applications with the resolved dependency set; replacing only SimpleWeather.dll is insufficient. This change removes SimpleWeather's Newtonsoft dependency, but other packages in an application may still require it.

## Service selection

Automatic selection retains existing One Call 3.0 access, then tries 4.0 and free endpoints on access denial. Explicit selection never silently changes services. The account must have access to the selected service, even when the same API key is used across services. See [service selection and request counts](library.md).
