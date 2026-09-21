// Copyright (c) 2026 Neil Colvin.
// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System;
using System.Globalization;




namespace SimpleWeather;

/// <summary>
/// Represents the current weather block returned by the One Call API.
/// </summary>
public class Current
	{
	/// <summary>Creates this model from its OpenWeather JSON fragment.</summary>
	/// <param name="json">The JSON fragment, or null for an empty model.</param>
	/// <returns>The parsed weather model.</returns>
	public static Current FromJson (string? json) => new (ResponseJson.ReadOptional<InstantResponse> (json));

	internal Current (InstantResponse? data)
		{
		if (data == null) return;
		DT = UnixToDateTime (data.DT ?? 0);
		Sunrise = UnixToDateTime (data.Sunrise ?? 0);
		Sunset = UnixToDateTime (data.Sunset ?? 0);
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
		Rain = new Rain (data.Rain);
		Snow = new Snow (data.Snow);
		Weather = new Weather (data.Weather);
		}

	/// <summary>
	/// Gets the timestamp of this reading converted to local time.
	/// </summary>
	public DateTime DT
		{
		get;
		}
	/// <summary>
	/// Gets the sunrise time associated with the reading.
	/// </summary>
	public DateTime Sunrise
		{
		get;
		}
	/// <summary>
	/// Gets the sunset time associated with the reading.
	/// </summary>
	public DateTime Sunset
		{
		get;
		}
	/// <summary>
	/// Gets the ambient temperature.
	/// </summary>
	public double? Temperature
		{
		get;
		}
	/// <summary>
	/// Gets the feels-like temperature that accounts for humidity and wind.
	/// </summary>
	public double? FeelsLike
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
	/// Gets the visibility distance in meters.
	/// </summary>
	public double? Visibility
		{
		get;
		}
	/// <summary>
	/// Gets the wind speed in meters per second.
	/// </summary>
	public double? WindSpeed
		{
		get;
		}
	/// <summary>
	/// Gets the wind gust speed in meters per second.
	/// </summary>
	public double? WindGust
		{
		get;
		}
	/// <summary>
	/// Gets the wind direction in meteorological degrees.
	/// </summary>
	public double? WindDegree
		{
		get;
		}
	/// <summary>
	/// Gets the short compass representation for the wind direction.
	/// </summary>
	public string? WindDirectionShort
		{
		get;
		}
	/// <summary>
	/// Gets the long compass representation for the wind direction.
	/// </summary>
	public string? WindDirectionLong
		{
		get;
		}
	/// <summary>
	/// Gets the rainfall totals for the period.
	/// </summary>
	public Rain? Rain
		{
		get;
		}
	/// <summary>
	/// Gets the snowfall totals for the period.
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

	private static DateTime UnixToDateTime (double unixTime)
		{
		var epoch = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		return epoch.AddSeconds (unixTime).ToLocalTime ();
		}
	}

