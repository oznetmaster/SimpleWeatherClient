// Copyright (c) 2026 Neil Colvin.
// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using static SimpleWeather.Utility;

namespace SimpleWeather;

/// <summary>
/// An object that provides weather forecast data
/// </summary>
public class WeatherForecast
	{
	private readonly int _timezoneOffsetSeconds;
	/// <summary>
	/// Initializes a new instance of the <see cref="WeatherForecast"/> class from a JSON response.
	/// </summary>
	/// <param name="jsonResponse">The raw JSON returned by the One Call API.</param>
	public WeatherForecast (string jsonResponse)
		{
		var data = JObject.Parse (jsonResponse);

        // 5 day /forecast returns { cod, list: [...], city: { coord, timezone, name, ... } }
        if (data.SelectToken ("list") is JArray list)
            {
            JToken? city = data.SelectToken ("city");
            JToken? coord = city?.SelectToken ("coord");
            Latitude = OptDouble (coord, "lat");
            Longitude = OptDouble (coord, "lon");
            Timezone = city?.SelectToken ("name")?.ToString ();
            _timezoneOffsetSeconds = OptInt (city, "timezone") ?? 0;
            TimezoneOffset = _timezoneOffsetSeconds / 3600;

            foreach (JToken item in list)
                {
                Hourly.Add (new Hourly (item));
                }

            BuildDailyFromForecast5 (list);
            return;
            }

        // One Call format
        Longitude = OptDouble (data, "lon");
		Latitude = OptDouble (data, "lat");
		Timezone = data.SelectToken ("timezone")?.ToString ();
		TimezoneOffset = (OptInt (data, "timezone_offset") ?? 0) / 3600;
		Current = new Current (data.SelectToken ("current"));

		JToken? hourlyData = data.SelectToken ("hourly") ?? throw new InvalidOperationException ("Hourly data is not available in the response.");
		foreach (JToken hour in hourlyData)
			{
			Hourly.Add (new Hourly (hour));
			}

		JToken? dailyData = data.SelectToken ("daily") ?? throw new InvalidOperationException ("Daily data is not available in the response.");
		foreach (JToken day in dailyData)
			{
			Daily.Add (new Daily (day));
			}

		JToken? alertsData = data.SelectToken ("alerts");
		if (alertsData != null)
			{
			Alerts = [];
			foreach (JToken alert in alertsData)
				{
				Alerts.Add (new Alerts (alert));
				}
			}
		}

	private void BuildDailyFromForecast5 (JArray list)
		{
		// Group 3-hour entries by local day. We don't have sunrise/sunset/etc; synthesize what we need.
		var groups = new Dictionary<DateTime, List<JToken>> ();
		foreach (JToken item in list)
			{
			var dtUnix = OptDouble (item, "dt") ?? 0;
            DateTime dt = DateTimeOffset.FromUnixTimeSeconds ((long)dtUnix).ToOffset (TimeSpan.FromSeconds (_timezoneOffsetSeconds)).Date;
			if (!groups.TryGetValue (dt, out List<JToken>? bucket))
				{
				bucket = [];
				groups[dt] = bucket;
				}

			bucket.Add (item);
			}

		foreach (KeyValuePair<DateTime, List<JToken>> kvp in groups)
			{
            List<JToken> bucket = kvp.Value;
			if (bucket.Count == 0)
				{
				continue;
				}

			double? min = null;
			double? max = null;
			double popMax = 0;
			JToken? chosenWeather = null;
			double closestToNoon = double.MaxValue;

			foreach (JToken item in bucket)
				{
				var tMin = OptDouble (item.SelectToken ("main"), "temp_min");
				var tMax = OptDouble (item.SelectToken ("main"), "temp_max");
				if (tMin.HasValue)
					{
					min = !min.HasValue ? tMin : Math.Min (min.Value, tMin.Value);
					}

				if (tMax.HasValue)
					{
					max = !max.HasValue ? tMax : Math.Max (max.Value, tMax.Value);
					}

				var pop = OptDouble (item, "pop") ?? 0;
				if (pop > popMax)
					{
					popMax = pop;
					}

				// Choose the forecast closest to midday as representative weather.
				var dtUnix = (long)(OptDouble (item, "dt") ?? 0);
                DateTimeOffset local = DateTimeOffset.FromUnixTimeSeconds (dtUnix).ToOffset (TimeSpan.FromSeconds (_timezoneOffsetSeconds));
				double minutesFromNoon = Math.Abs ((local.TimeOfDay - TimeSpan.FromHours (12)).TotalMinutes);
				if (item.SelectToken ("weather") is JToken weather && minutesFromNoon < closestToNoon)
					{
					chosenWeather = weather;
					closestToNoon = minutesFromNoon;
					}
				}

			var dayUnix = new DateTimeOffset (kvp.Key, TimeSpan.FromSeconds (_timezoneOffsetSeconds)).ToUnixTimeSeconds ();
			var dailyObj = new JObject
				{
				["dt"] = dayUnix,
				["temp"] = new JObject
					{
					["min"] = min,
					["max"] = max
					},
				["pop"] = popMax,
				["weather"] = chosenWeather != null ? JToken.FromObject (chosenWeather) : null
				};

			Daily.Add (new Daily (dailyObj));
			}
		}

	/// <summary>
	/// Geographical coordinates of the location (latitude)
	/// </summary>
	public double? Latitude { get; }
	/// <summary>
	/// Geographical coordinates of the location (longitude)
	/// </summary>
	public double? Longitude { get; }
	/// <summary>
	/// Timezone name for the requested location
	/// </summary>
	public string? Timezone { get; }
	/// <summary>
	/// Shift in hours from UTC
	/// </summary>
	public int TimezoneOffset { get; }
	/// <summary>
	/// Current weather data as an object
	/// </summary>
	public Current? Current { get; }
	/// <summary>
	/// Hourly forecast weather data as a list of objects containing the data for each hour
	/// </summary>
	public List<Hourly> Hourly { get; } = [];
	/// <summary>
	/// Daily forecast weather data as a list of objects containing the data for each day
	/// </summary>
	public List<Daily> Daily { get; } = [];
	/// <summary>
	/// A list of objects containing National weather alerts data from major national weather warning systems
	/// </summary>
	public List<Alerts> Alerts { get; } = [];
	}

