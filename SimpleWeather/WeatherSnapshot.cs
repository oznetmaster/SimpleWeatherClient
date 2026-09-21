// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

namespace SimpleWeather;

/// <summary>Current conditions and a forecast retrieved together to avoid duplicate One Call requests.</summary>
public sealed class WeatherSnapshot
	{
	internal WeatherSnapshot (CurrentWeather currentWeather, WeatherForecast forecast)
		{
		CurrentWeather = currentWeather;
		Forecast = forecast;
		}

	/// <summary>Current conditions for the requested location.</summary>
	public CurrentWeather CurrentWeather { get; }

	/// <summary>The forecast. Hourly readings are empty when they were not requested.</summary>
	public WeatherForecast Forecast { get; }
	}
