// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using Newtonsoft.Json.Linq;

using static SimpleWeather.Utility;

namespace SimpleWeather;

/// <summary>
/// Represents snowfall data for the current weather response.
/// </summary>
public class Snow
	{
	/// <summary>
	/// Initializes a new instance of the <see cref="Snow"/> class from JSON data.
	/// </summary>
	/// <param name="data">The JSON token that holds snow metrics.</param>
	public Snow (JToken? data)
		{
		if (data != null)
			{
			OneHour = OptDouble (data, "1h");
			ThreeHours = OptDouble (data, "3h");
			}
		}

	/// <summary>
	/// Snow volume for the last 1 hour, mm
	/// </summary>
	public double? OneHour
		{
		get;
		}
	/// <summary>
	/// Snow volume for the last 3 hour, mm
	/// </summary>
	public double? ThreeHours
		{
		get;
		}
	}

