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
	/// <value>The timestamp of this reading converted to local time.</value>
	public DateTime DT
		{
		get;
		}
	/// <summary>
	/// Gets the sunrise time associated with the reading.
	/// </summary>
	/// <value>The sunrise time associated with the reading.</value>
	public DateTime Sunrise
		{
		get;
		}
	/// <summary>
	/// Gets the sunset time associated with the reading.
	/// </summary>
	/// <value>The sunset time associated with the reading.</value>
	public DateTime Sunset
		{
		get;
		}
	/// <summary>
	/// Gets the ambient temperature.
	/// </summary>
	/// <value>The ambient temperature.</value>
	public double? Temperature
		{
		get;
		}
	/// <summary>
	/// Gets the feels-like temperature that accounts for humidity and wind.
	/// </summary>
	/// <value>The feels-like temperature that accounts for humidity and wind.</value>
	public double? FeelsLike
		{
		get;
		}
	/// <summary>
	/// Gets the sea-level atmospheric pressure in hPa.
	/// </summary>
	/// <value>The sea-level atmospheric pressure in hPa.</value>
	public double? Pressure
		{
		get;
		}
	/// <summary>
	/// Gets the relative humidity percentage.
	/// </summary>
	/// <value>The relative humidity percentage.</value>
	public double? Humidity
		{
		get;
		}
	/// <summary>
	/// Gets the dew point temperature.
	/// </summary>
	/// <value>The dew point temperature.</value>
	public double? DewPoint
		{
		get;
		}
	/// <summary>
	/// Gets the cloud coverage percentage.
	/// </summary>
	/// <value>The cloud coverage percentage.</value>
	public double? Clouds
		{
		get;
		}
	/// <summary>
	/// Gets the UV index.
	/// </summary>
	/// <value>The UV index.</value>
	public double? Uvi
		{
		get;
		}
	/// <summary>
	/// Gets the visibility distance in meters.
	/// </summary>
	/// <value>The visibility distance in meters.</value>
	public double? Visibility
		{
		get;
		}
	/// <summary>
	/// Gets the wind speed in meters per second.
	/// </summary>
	/// <value>The wind speed in meters per second.</value>
	public double? WindSpeed
		{
		get;
		}
	/// <summary>
	/// Gets the wind gust speed in meters per second.
	/// </summary>
	/// <value>The wind gust speed in meters per second.</value>
	public double? WindGust
		{
		get;
		}
	/// <summary>
	/// Gets the wind direction in meteorological degrees.
	/// </summary>
	/// <value>The wind direction in meteorological degrees.</value>
	public double? WindDegree
		{
		get;
		}
	/// <summary>
	/// Gets the short compass representation for the wind direction.
	/// </summary>
	/// <value>The short compass representation for the wind direction.</value>
	public string? WindDirectionShort
		{
		get;
		}
	/// <summary>
	/// Gets the long compass representation for the wind direction.
	/// </summary>
	/// <value>The long compass representation for the wind direction.</value>
	public string? WindDirectionLong
		{
		get;
		}
	/// <summary>
	/// Gets the rainfall totals for the period.
	/// </summary>
	/// <value>The rainfall totals for the period.</value>
	public Rain? Rain
		{
		get;
		}
	/// <summary>
	/// Gets the snowfall totals for the period.
	/// </summary>
	/// <value>The snowfall totals for the period.</value>
	public Snow? Snow
		{
		get;
		}
	/// <summary>
	/// Gets the descriptive weather information.
	/// </summary>
	/// <value>The descriptive weather information.</value>
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

