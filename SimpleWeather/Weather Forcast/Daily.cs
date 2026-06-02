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
/// Represents a single day of forecast data from the One Call API.
/// </summary>
public class Daily
	{
	/// <summary>
	/// Initializes a new instance of the <see cref="Daily"/> class from JSON data.
	/// </summary>
	/// <param name="data">The JSON token that contains the daily forecast.</param>
	public Daily (JToken? data)
		{
		if (data != null)
			{
			DT = UnixToDateTime (double.Parse (data.SelectToken ("dt")?.ToString () ?? "0", CultureInfo.InvariantCulture));
			Sunrise = UnixToDateTime (OptDouble (data, "sunrise"));
			Sunset = UnixToDateTime (OptDouble (data, "sunset"));
			Moonrise = UnixToDateTime (OptDouble (data, "moonrise"));
			Moonset = UnixToDateTime (OptDouble (data, "moonset"));
			MoonPhase = DoubleToMoonPhase (OptDouble (data, "moon_phase"));
			Temperature = new Temperature (data.SelectToken ("temp"));
			FeelsLike = new FeelsLike (data.SelectToken ("feels_like"));
			Pressure = OptDouble (data, "pressure");
			Humidity = OptDouble (data, "humidity");
			DewPoint = OptDouble (data, "dew_point");
			WindSpeed = OptDouble (data, "wind_speed");
			WindGust = OptDouble (data, "wind_gust");
			WindDegree = OptDouble (data, "wind_deg");
			WindDirectionShort = Wind.GetWindDirectionShort (WindDegree);
			WindDirectionLong = Wind.GetWindDirectionLong (WindDegree);
			Clouds = OptDouble (data, "clouds");
			Uvi = OptDouble (data, "uvi");
			PrecipitationProbability = Math.Round ((OptDouble (data, "pop") ?? 0) * 100);
			Rain = OptDouble (data, "rain");
			Snow = OptDouble (data, "snow");
			Weather = new Weather (data.SelectToken ("weather"));
			}
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

