// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using Newtonsoft.Json.Linq;

using static SimpleWeather.Utility;

namespace SimpleWeather;

/// <summary>
/// Represents the temperature readings for each part of the day in the daily forecast.
/// </summary>
public class Temperature
	{
	/// <summary>
	/// Initializes a new instance of the <see cref="Temperature"/> class from JSON data.
	/// </summary>
	/// <param name="data">The JSON token that contains the temperature values.</param>
	public Temperature (JToken? data)
		{
		if (data != null)
			{
			Morning = OptDouble (data, "morn");
			Day = OptDouble (data, "day");
			Evening = OptDouble (data, "eve");
			Night = OptDouble (data, "night");
			Min = OptDouble (data, "min");
			Max = OptDouble (data, "max");
			}
		}

	/// <summary>
	/// Morning temperature in Celsius.
	/// </summary>
	public double? Morning { get; }
	/// <summary>
	/// Daytime temperature in Celsius.
	/// </summary>
	public double? Day { get; }
	/// <summary>
	/// Evening temperature in Celsius.
	/// </summary>
	public double? Evening { get; }
	/// <summary>
	/// Night temperature in Celsius.
	/// </summary>
	public double? Night { get; }
	/// <summary>
	/// Minimum daily temperature in Celsius.
	/// </summary>
	public double? Min { get; }
	/// <summary>
	/// Maximum daily temperature in Celsius.
	/// </summary>
	public double? Max { get; }
	}

