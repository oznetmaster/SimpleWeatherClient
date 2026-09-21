// Copyright (c) 2026 Neil Colvin.
// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System;
using System.Collections.Generic;





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
	public WeatherForecast (string jsonResponse) : this (ResponseJson.Read<WeatherResponse> (jsonResponse)) { }

	internal WeatherForecast (WeatherResponse data, bool includeHourly = true)
		{
		if (data.List != null)
			{
			Latitude = data.City?.Coordinates?.Latitude;
			Longitude = data.City?.Coordinates?.Longitude;
			Timezone = data.City?.Name;
			_timezoneOffsetSeconds = (int)(data.City?.Timezone ?? 0);
			TimezoneOffset = _timezoneOffsetSeconds / 3600;
			if (includeHourly) foreach (InstantResponse item in data.List) Hourly.Add (new Hourly (item));
			BuildDailyFromForecast5 (data.List);
			return;
			}
		Longitude = data.Longitude;
		Latitude = data.Latitude;
		Timezone = data.Timezone;
		TimezoneOffset = (int)(data.TimezoneOffset ?? 0) / 3600;
		Current = new Current (data.Current);
		if (includeHourly)
			foreach (InstantResponse hour in data.Hourly ?? throw new InvalidOperationException ("Hourly data is not available in the response.")) Hourly.Add (new Hourly (hour));
		foreach (DailyResponse day in data.Daily ?? throw new InvalidOperationException ("Daily data is not available in the response.")) Daily.Add (new Daily (day));
		foreach (AlertResponse alert in data.Alerts ?? []) Alerts.Add (new Alerts (alert));
		}

	private void BuildDailyFromForecast5 (InstantResponse[] list)
		{
		var groups = new Dictionary<DateTime, List<InstantResponse>> ();
		foreach (InstantResponse item in list)
			{
			DateTime day = DateTimeOffset.FromUnixTimeSeconds ((long)(item.DT ?? 0)).ToOffset (TimeSpan.FromSeconds (_timezoneOffsetSeconds)).Date;
			if (!groups.TryGetValue (day, out List<InstantResponse>? bucket)) groups[day] = bucket = [];
			bucket.Add (item);
			}
		foreach (KeyValuePair<DateTime, List<InstantResponse>> pair in groups)
			{
			double? min = null, max = null;
			double popMax = 0, closestToNoon = double.MaxValue;
			ConditionResponse[]? weather = null;
			foreach (InstantResponse item in pair.Value)
				{
				double? low = item.Main?.TemperatureMin, high = item.Main?.TemperatureMax;
				if (low.HasValue) min = min.HasValue ? Math.Min (min.Value, low.Value) : low;
				if (high.HasValue) max = max.HasValue ? Math.Max (max.Value, high.Value) : high;
				popMax = Math.Max (popMax, item.PrecipitationProbability ?? 0);
				DateTimeOffset local = DateTimeOffset.FromUnixTimeSeconds ((long)(item.DT ?? 0)).ToOffset (TimeSpan.FromSeconds (_timezoneOffsetSeconds));
				double distance = Math.Abs ((local.TimeOfDay - TimeSpan.FromHours (12)).TotalMinutes);
				if (item.Weather != null && distance < closestToNoon) { weather = item.Weather; closestToNoon = distance; }
				}
			Daily.Add (new Daily (new DailyResponse
				{
				DT = new DateTimeOffset (pair.Key, TimeSpan.FromSeconds (_timezoneOffsetSeconds)).ToUnixTimeSeconds (),
				Temperature = new TemperatureResponse { Min = min, Max = max },
				PrecipitationProbability = popMax,
				Weather = weather
				}));
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

