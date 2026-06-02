// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using Newtonsoft.Json.Linq;

using static SimpleWeather.Utility;

namespace SimpleWeather;

/// <summary>
/// Represents the primary temperature- and pressure-related section of the weather payload.
/// </summary>
public class Main
	{
	/// <summary>
	/// Initializes a new instance of the <see cref="Main"/> class from JSON data.
	/// </summary>
	/// <param name="data">The JSON token that contains the main weather metrics.</param>
	public Main (JToken? data)
		{
		if (data != null)
			{
			Temperature = OptDouble (data, "temp");
			FeelsLike = OptDouble (data, "feels_like");
			TemperatureMin = OptDouble (data, "temp_min");
			TemperatureMax = OptDouble (data, "temp_max");
			Pressure = OptDouble (data, "pressure");
			Humidity = OptDouble (data, "humidity");
			SeaLevel = OptDouble (data, "sea_level");
			GroundLevel = OptDouble (data, "grnd_level");
			DewPoint = OptDouble (data, "dew_point");
			}
		}

	/// <summary>
	/// Temperature. Default Unit: Celsius
	/// </summary>
	public double? Temperature
		{
		get;
		}
	/// <summary>
	/// Temperature. This temperature parameter accounts for the human perception of weather. Default Unit: Celsius
	/// </summary>
	public double? FeelsLike
		{
		get;
		}
	/// <summary>
	/// Minimum temperature at the moment. This is minimal currently observed temperature (within large megalopolises and urban areas). Default Unit: Celsius
	/// </summary>
	public double? TemperatureMin
		{
		get;
		}
	/// <summary>
	/// Maximum temperature at the moment. This is maximal currently observed temperature (within large megalopolises and urban areas). Default Unit: Celsius
	/// </summary>
	public double? TemperatureMax
		{
		get;
		}
	/// <summary>
	/// Atmospheric pressure (on the sea level, if there is no SeaLevel or GroundLevel data), hPa
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
	/// Atmospheric pressure on the sea level, hPa
	/// </summary>
	public double? SeaLevel
		{
		get;
		}
	/// <summary>
	/// Atmospheric pressure on the ground level, hPa
	/// </summary>
	public double? GroundLevel
		{
		get;
		}
	/// <summary>
	/// Dew point temperature in Celsius.
	/// </summary>
	public double? DewPoint
		{
		get;
		}
	}

