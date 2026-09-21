// Copyright (c) 2026 Neil Colvin.
// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System;




namespace SimpleWeather;

/// <summary>
/// Represents a single hourly forecast entry from the One Call API.
/// </summary>
public class Hourly
	{
	/// <summary>Creates this model from its OpenWeather JSON fragment.</summary>
	/// <param name="json">The JSON fragment, or null for an empty model.</param>
	/// <returns>The parsed weather model.</returns>
	public static Hourly FromJson (string? json) => new (ResponseJson.ReadOptional<InstantResponse> (json));

	internal Hourly (InstantResponse? data)
		{
		if (data == null) return;
		DT = UnixToDateTime (data.DT ?? 0);
		MainResponse readings = data.Main ?? data;
		Temperature = readings.Temperature;
		FeelsLike = readings.FeelsLike;
		Pressure = readings.Pressure;
		Humidity = readings.Humidity;
		DewPoint = data.DewPoint;
		Uvi = data.Uvi;
		Clouds = data.Clouds;
		Visibility = data.Visibility;
		WindSpeed = data.WindSpeed ?? data.Wind?.Speed;
		WindDegree = data.WindDegree ?? data.Wind?.Degree;
		WindGust = data.WindGust ?? data.Wind?.Gust;
		WindDirectionShort = Wind.GetWindDirectionShort (WindDegree);
		WindDirectionLong = Wind.GetWindDirectionLong (WindDegree);
		PrecipitationProbability = Math.Round ((data.PrecipitationProbability ?? 0) * 100);
		Rain = new Rain (data.Rain);
		Snow = new Snow (data.Snow);
		Weather = new Weather (data.Weather);
		}

	/// <summary>
	/// Time of the forecasted data, UTC
	/// </summary>
	public DateTime? DT
		{
		get;
		}
	/// <summary>
	/// Temperature
	/// </summary>
	public double? Temperature
		{
		get;
		}
	/// <summary>
	/// Temperature. This accounts for the human perception of weather.
	/// </summary>
	public double? FeelsLike
		{
		get;
		}
	/// <summary>
	/// Atmospheric pressure on the sea level, hPa
	/// </summary>
	public double? Pressure
		{
		get;
		}
	/// <summary>
	/// Humidity, %
	/// </summary>
	public double? Humidity
		{
		get;
		}
	/// <summary>
	///  Atmospheric temperature (varying according to pressure and humidity) below which water droplets begin to condense and dew can form.
	/// </summary>
	public double? DewPoint
		{
		get;
		}
	/// <summary>
	/// UV index
	/// </summary>
	public double? Uvi
		{
		get;
		}
	/// <summary>
	/// Cloudiness, %
	/// </summary>
	public double? Clouds
		{
		get;
		}
	/// <summary>
	/// Visibility in meters.
	/// </summary>
	public double? Visibility
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
	/// Probability of precipitation in percents. The values of the parameter vary between 0 and 100
	/// </summary>
	public double? PrecipitationProbability
		{
		get;
		}
	/// <summary>
	/// Gets the rainfall totals for the hour.
	/// </summary>
	public Rain? Rain
		{
		get;
		}
	/// <summary>
	/// Gets the snowfall totals for the hour.
	/// </summary>
	public Snow? Snow
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

	private static DateTime UnixToDateTime (double? unixTime)
		{
		var epoch = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		return epoch.AddSeconds (unixTime ?? 0).ToLocalTime ();
		}
	}

