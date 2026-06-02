// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System;
using System.Globalization;

using Newtonsoft.Json.Linq;

namespace SimpleWeather;

/// <summary>
/// Represents a single weather alert returned by the One Call API.
/// </summary>
public class Alerts
	{
	/// <summary>
	/// Initializes a new instance of the <see cref="Alerts"/> class from JSON data.
	/// </summary>
	/// <param name="data">The JSON token that contains the alert information.</param>
	public Alerts (JToken? data)
		{
		if (data != null)
			{
			SenderName = data.SelectToken ("sender_name")?.ToString ();
			Event = data.SelectToken ("event")?.ToString ();
			Start = UnixToDateTime (double.Parse (data.SelectToken ("start")?.ToString () ?? "0", CultureInfo.InvariantCulture));
			End = UnixToDateTime (double.Parse (data.SelectToken ("end")?.ToString () ?? "0", CultureInfo.InvariantCulture));
			Description = data.SelectToken ("description")?.ToString ();
			Tags = data.SelectToken ("tags")?.ToString ();
			}
		}

	/// <summary>
	/// Gets the source agency issuing the alert.
	/// </summary>
	public string? SenderName { get; }
	/// <summary>
	/// Gets the alert event name.
	/// </summary>
	public string? Event { get; }
	/// <summary>
	/// Gets the local start time of the alert window.
	/// </summary>
	public DateTime Start { get; }
	/// <summary>
	/// Gets the local end time of the alert window.
	/// </summary>
	public DateTime End { get; }
	/// <summary>
	/// Gets the textual description of the alert.
	/// </summary>
	public string? Description { get; }
	/// <summary>
	/// Gets the raw tags supplied by the API.
	/// </summary>
	public string? Tags { get; }

	private static DateTime UnixToDateTime (double unixTime)
		{
		var epoch = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		return epoch.AddSeconds (unixTime).ToLocalTime ();
		}
	}

