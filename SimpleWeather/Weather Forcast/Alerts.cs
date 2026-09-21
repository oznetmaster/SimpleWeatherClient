// Copyright (c) 2026 Neil Colvin.
// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System;
using System.Globalization;


namespace SimpleWeather;

/// <summary>
/// Represents a single weather alert returned by the One Call API.
/// </summary>
public class Alerts
	{
	/// <summary>Creates this model from its OpenWeather JSON fragment.</summary>
	/// <param name="json">The JSON fragment, or null for an empty model.</param>
	/// <returns>The parsed weather model.</returns>
	public static Alerts FromJson (string? json) => new (ResponseJson.ReadOptional<AlertResponse> (json));

	internal Alerts (AlertResponse? data)
		{
		if (data == null) return;
		SenderName = data.SenderName;
		Event = data.Event;
		Start = UnixToDateTime (data.Start ?? 0);
		End = UnixToDateTime (data.End ?? 0);
		Description = data.Description;
		Tags = data.Tags == null ? null : System.Text.Json.JsonSerializer.Serialize (data.Tags);
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

