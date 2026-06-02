// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using Newtonsoft.Json.Linq;

using System;
using System.Globalization;

using static SimpleWeather.Utility;

namespace SimpleWeather;

/// <summary>
/// Encapsulates system level metadata returned by the current weather API.
/// </summary>
public class Sys
	{
	/// <summary>
	/// Initializes a new instance of the <see cref="Sys"/> class from JSON data.
	/// </summary>
	/// <param name="data">The JSON token that contains the system information.</param>
	public Sys (JToken? data)
		{
		if (data != null)
			{
			Type = OptInt (data, "type");
			ID = OptInt (data, "id");
			Country = data.SelectToken ("country")?.ToString ();
			Sunrise = UnixToDateTime (OptDouble (data, "sunrise"));
			Sunset = UnixToDateTime (OptDouble (data, "sunset"));
			SunriseTime = DateTimeToSimpleTime (Sunrise);
			SunsetTime = DateTimeToSimpleTime (Sunset);
			}
		}

	/// <summary>
	/// Internal parameter returned by OpenWeather.
	/// </summary>
	public int? Type
		{
		get;
		}
	/// <summary>
	/// Unique identifier for the system block.
	/// </summary>
	public int? ID
		{
		get;
		}
	/// <summary>
	/// Country code (ISO 3166) of the location.
	/// </summary>
	public string? Country
		{
		get;
		}
	/// <summary>
	/// A DateTime object representing sunrise time converted to your local time
	/// </summary>
	public DateTime Sunrise
		{
		get;
		}
	/// <summary>
	/// Time of sunrise in the format HH:mm
	/// </summary>
	public string? SunriseTime
		{
		get;
		}
	/// <summary>
	/// A DateTime object representing sunset time converted to your local time
	/// </summary>
	public DateTime Sunset
		{
		get;
		}
	/// <summary>
	/// Time of sunset in the format HH:mm
	/// </summary>
	public string? SunsetTime
		{
		get;
		}

	private static DateTime UnixToDateTime (double? unixTime)
		{
		DateTime epoch = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		return epoch.AddSeconds (unixTime ?? 0).ToLocalTime ();
		}

	private static string DateTimeToSimpleTime (DateTime dateTime) => dateTime.ToString ("t", CultureInfo.InvariantCulture);
	}

