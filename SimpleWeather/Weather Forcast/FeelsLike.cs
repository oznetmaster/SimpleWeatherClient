// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using Newtonsoft.Json.Linq;

using static SimpleWeather.Utility;

namespace SimpleWeather;

/// <summary>
/// Represents the perceived temperature for each part of the day in the daily forecast.
/// </summary>
public class FeelsLike
	{
	/// <summary>
	/// Initializes a new instance of the <see cref="FeelsLike"/> class from JSON data.
	/// </summary>
	/// <param name="data">The JSON token that contains the feels-like values.</param>
	public FeelsLike (JToken? data)
		{
		if (data != null)
			{
			Morning = OptDouble (data, "morn");
			Day = OptDouble (data, "day");
			Evening = OptDouble (data, "eve");
			Night = OptDouble (data, "night");
			}
		}

	/// <summary>
	/// Morning feels-like temperature in Celsius.
	/// </summary>
	public double? Morning { get; }
	/// <summary>
	/// Daytime feels-like temperature in Celsius.
	/// </summary>
	public double? Day { get; }
	/// <summary>
	/// Evening feels-like temperature in Celsius.
	/// </summary>
	public double? Evening { get; }
	/// <summary>
	/// Night feels-like temperature in Celsius.
	/// </summary>
	public double? Night { get; }
	}

