// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System;
using System.Globalization;

using Newtonsoft.Json.Linq;

using static SimpleWeather.Utility;

namespace SimpleWeather;

/// <summary>
/// Represents the current weather block returned by the One Call API.
/// </summary>
public class Current
	{
	/// <summary>
	/// Initializes a new instance of the <see cref="Current"/> class from JSON data.
	/// </summary>
	/// <param name="data">The JSON token that contains the current weather values.</param>
	public Current (JToken? data)
		{
		if (data != null)
			{
			DT = UnixToDateTime (double.Parse (data.SelectToken ("dt")?.ToString () ?? "0", CultureInfo.InvariantCulture));
			Sunrise = UnixToDateTime (double.Parse (data.SelectToken ("sunrise")?.ToString () ?? "0", CultureInfo.InvariantCulture));
			Sunset = UnixToDateTime (double.Parse (data.SelectToken ("sunset")?.ToString () ?? "0", CultureInfo.InvariantCulture));
			Temperature = OptDouble (data, "temp");
			FeelsLike = OptDouble (data, "feels_like");
			Pressure = OptDouble (data, "pressure");
			Humidity = OptDouble (data, "humidity");
			DewPoint = OptDouble (data, "dew_point");
			Clouds = OptDouble (data, "clouds");
			Uvi = OptDouble (data, "uvi");
			Visibility = OptDouble (data, "visibility");
			WindSpeed = OptDouble (data, "wind_speed");
			WindGust = OptDouble (data, "wind_gust");
			WindDegree = OptDouble (data, "wind_deg");
			WindDirectionShort = Wind.GetWindDirectionShort (WindDegree);
			WindDirectionLong = Wind.GetWindDirectionLong (WindDegree);
			Rain = new Rain (data.SelectToken ("rain"));
			Snow = new Snow (data.SelectToken ("snow"));
			Weather = new Weather (data.SelectToken ("weather"));
			}
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

