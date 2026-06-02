# Getting Started

The published API site is generated reference documentation for the `SimpleWeather` library. The most important entry points are:

- [WeatherController](../api/SimpleWeather.WeatherController.yml)
- [GeoLocator](../api/SimpleWeather.GeoLocator.yml)
- [CurrentWeather](../api/SimpleWeather.CurrentWeather.yml)
- [WeatherForecast](../api/SimpleWeather.WeatherForecast.yml)
- [LatLong](../api/SimpleWeather.LatLong.yml)

## Typical Flow

1. Create a `WeatherController`
2. Supply an OpenWeather API key
3. Request current weather or forecast data
4. Read the returned strongly typed models

## Example

```csharp
using SimpleWeather;

var controller = new WeatherController("YOUR_API_KEY");
var current = await controller.GetCurrentWeatherAsync(new LatLong(-32.07019, 115.95726));
var forecast = await controller.GetWeatherForecastAsync(new LatLong(-32.07019, 115.95726));
```

## Next Pages

- [Overview](overview.md)
- [API Reference](../api/toc.yml)
