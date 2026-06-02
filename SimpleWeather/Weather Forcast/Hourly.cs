// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System;

using Newtonsoft.Json.Linq;

using static SimpleWeather.Utility;  

namespace SimpleWeather;

/// <summary>
/// Represents a single hourly forecast entry from the One Call API.
/// </summary>
public class Hourly
	{
	/// <summary>
	/// Initializes a new instance of the <see cref="Hourly"/> class from JSON data.
	/// </summary>
	/// <param name="data">The JSON token that contains the hourly forecast.</param>
	public Hourly (JToken? data)
		{
		if (data != null)
			{
			// DateTime
			DT = UnixToDateTime (OptDouble (data, "dt"));
			Temperature = OptDouble (data, "temp");
			FeelsLike = OptDouble (data, "feels_like");
			Pressure = OptDouble (data, "pressure");
			Humidity = OptDouble (data, "humidity");
			DewPoint = OptDouble (data, "dew_point");
			Uvi = OptDouble (data, "uvi");
			Clouds = OptDouble (data, "clouds");
			Visibility = OptDouble (data, "visibility");
			WindSpeed = OptDouble (data, "wind_speed");
			WindDirectionShort = Wind.GetWindDirectionShort (WindDegree);
			WindDirectionLong = Wind.GetWindDirectionLong (WindDegree);
			WindGust = OptDouble (data, "wind_gust");
			WindDegree = OptDouble (data, "wind_deg");
			PrecipitationProbability = Math.Round (OptDouble (data, "pop") ?? 0 * 100);
			Rain = new Rain (data.SelectToken ("rain"));
			Snow = new Snow (data.SelectToken ("snow"));
			Weather = new Weather (data.SelectToken ("weather"));
			}
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

