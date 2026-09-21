// Copyright (c) 2026 Neil Colvin.
// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System;
using System.Globalization;




namespace SimpleWeather;

/// <summary>
/// Represents a single day of forecast data from the One Call API.
/// </summary>
public class Daily
	{
	/// <summary>Creates this model from its OpenWeather JSON fragment.</summary>
	/// <param name="json">The JSON fragment, or null for an empty model.</param>
	/// <returns>The parsed weather model.</returns>
	public static Daily FromJson (string? json) => new (ResponseJson.ReadOptional<DailyResponse> (json));

	internal Daily (DailyResponse? data)
		{
		if (data == null) return;
		DT = UnixToDateTime (data.DT ?? 0);
		Sunrise = UnixToDateTime (data.Sunrise ?? 0);
		Sunset = UnixToDateTime (data.Sunset ?? 0);
		Moonrise = UnixToDateTime (data.Moonrise ?? 0);
		Moonset = UnixToDateTime (data.Moonset ?? 0);
		MoonPhase = DoubleToMoonPhase (data.MoonPhase);
		Temperature = new Temperature (data.Temperature);
		FeelsLike = new FeelsLike (data.FeelsLike);
		Pressure = data.Pressure;
		Humidity = data.Humidity;
		DewPoint = data.DewPoint;
		WindSpeed = data.WindSpeed;
		WindDegree = data.WindDegree;
		WindGust = data.WindGust;
		Clouds = data.Clouds;
		Uvi = data.Uvi;
		Rain = data.Rain;
		Snow = data.Snow;
		WindDirectionShort = Wind.GetWindDirectionShort (WindDegree);
		WindDirectionLong = Wind.GetWindDirectionLong (WindDegree);
		PrecipitationProbability = Math.Round ((data.PrecipitationProbability ?? 0) * 100);
		Weather = new Weather (data.Weather);
		}

	/// <summary>
	/// Gets the forecast timestamp converted to local time.
	/// </summary>
	public DateTime? DT
		{
		get;
		}
	/// <summary>
	/// Gets the sunrise time for the forecast day.
	/// </summary>
	public DateTime? Sunrise
		{
		get;
		}
	/// <summary>
	/// Gets the sunset time for the forecast day.
	/// </summary>
	public DateTime? Sunset
		{
		get;
		}
	/// <summary>
	/// Gets the moonrise time for the forecast day.
	/// </summary>
	public DateTime? Moonrise
		{
		get;
		}
	/// <summary>
	/// Gets the moonset time for the forecast day.
	/// </summary>
	public DateTime? Moonset
		{
		get;
		}
	/// <summary>
	/// Moon phase
	/// </summary>
	public string? MoonPhase
		{
		get;
		}
	/// <summary>
	/// Gets the day-parted temperatures.
	/// </summary>
	public Temperature? Temperature
		{
		get;
		}
	/// <summary>
	/// Gets the day-parted feels-like temperatures.
	/// </summary>
	public FeelsLike? FeelsLike
		{
		get;
		}
	/// <summary>
	/// Gets the sea-level atmospheric pressure in hPa.
	/// </summary>
	public double? Pressure
		{
		get;
		}
	/// <summary>
	/// Gets the relative humidity percentage.
	/// </summary>
	public double? Humidity
		{
		get;
		}
	/// <summary>
	/// Gets the dew point temperature.
	/// </summary>
	public double? DewPoint
		{
		get;
		}
	/// <summary>
	/// Wind speed. Default Unit: meter/sec
	/// </summary>
	public double? WindSpeed
		{
		get;
		}
	/// <summary>
	/// Wind gust. Default Unit: meter/sec
	/// </summary>
	public double? WindGust
		{
		get;
		}
	/// <summary>
	/// Wind direction, degrees (meteorological)
	/// </summary>
	public double? WindDegree
		{
		get;
		}
	/// <summary>
	/// Short wind direction represented by a string (N, E, S, W, etc.)
	/// </summary>
	public string? WindDirectionShort
		{
		get;
		}
	/// <summary>
	/// Long wind direction represented by a string (North, East, South, West, etc.)
	/// </summary>
	public string? WindDirectionLong
		{
		get;
		}
	/// <summary>
	/// Gets the cloud coverage percentage.
	/// </summary>
	public double? Clouds
		{
		get;
		}
	/// <summary>
	/// Gets the UV index.
	/// </summary>
	public double? Uvi
		{
		get;
		}
	/// <summary>
	/// Probability of precipitation in percents. The values of the parameter vary between 0 and 100
	/// </summary>
	public double? PrecipitationProbability
		{
		get;
		}
	/// <summary>
	/// Gets the total rainfall in millimeters.
	/// </summary>
	public double? Rain
		{
		get;
		}
	/// <summary>
	/// Gets the total snowfall in millimeters.
	/// </summary>
	public double? Snow
		{
		get;
		}
	/// <summary>
	/// Gets the descriptive weather information.
	/// </summary>
	public Weather? Weather
		{
		get;
		}

	private static DateTime? UnixToDateTime (double? unixTime)
		{
		if (unixTime == null)
			{
			return null;
			}

		var epoch = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		return epoch.AddSeconds (unixTime.Value).ToLocalTime ();
		}

	private static string DoubleToMoonPhase (double? phase)
		{
		var moonPhase = string.Empty;

		if (phase == null)
			{
			return moonPhase;
			}

		switch (phase)
			{
			case >= 0.000 and <= 0.062:
				moonPhase = "New Moon";
				break;
			case > 0.062 and < 0.187:
				moonPhase = "Waxing Crescent";
				break;
			case >= 0.187 and <= 0.312:
				moonPhase = "First Quarter";
				break;
			case > 0.312 and < 0.437:
				moonPhase = "Waxing Gibbous";
				break;
			case >= 0.437 and <= 0.562:
				moonPhase = "Full Moon";
				break;
			case > 0.562 and < 0.687:
				moonPhase = "Waning Gibbous";
				break;
			case >= 0.687 and <= 0.812:
				moonPhase = "Last Quarter";
				break;
			case > 0.812 and < 0.938:
				moonPhase = "Waning Crescent";
				break;
			case >= 0.938 and <= 1.000:
				moonPhase = "New Moon";
				break;
			default:
				break;
			}

		return moonPhase;
		}
	}

