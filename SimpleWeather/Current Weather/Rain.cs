// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using Newtonsoft.Json.Linq;

using static SimpleWeather.Utility;

namespace SimpleWeather;

/// <summary>
/// Represents precipitation data for the current weather response.
/// </summary>
public class Rain
	{
	/// <summary>
	/// Initializes a new instance of the <see cref="Rain"/> class from JSON data.
	/// </summary>
	/// <param name="data">The JSON token containing rain volume metrics.</param>
	public Rain (JToken? data)
		{
		if (data != null)
			{
			OneHour = OptDouble (data, "1h");
			ThreeHours = OptDouble (data, "3h");
			}
		}
	/// <summary>
	/// Rain volume for the last 1 hour, mm
	/// </summary>
	public double? OneHour
		{
		get;
		}
	/// <summary>
	/// Rain volume for the last 3 hours, mm
	/// </summary>
	public double? ThreeHours
		{
		get;
		}
	}

