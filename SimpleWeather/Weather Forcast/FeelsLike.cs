// Copyright (c) 2026 Neil Colvin.
// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)




namespace SimpleWeather;

/// <summary>
/// Represents the perceived temperature for each part of the day in the daily forecast.
/// </summary>
public class FeelsLike
	{
	/// <summary>Creates this model from its OpenWeather JSON fragment.</summary>
	/// <param name="json">The JSON fragment, or null for an empty model.</param>
	/// <returns>The parsed weather model.</returns>
	public static FeelsLike FromJson (string? json) => new (ResponseJson.ReadOptional<TemperatureResponse> (json));

	internal FeelsLike (TemperatureResponse? data)
		{
		if (data == null) return;
		Morning = data.Morning;
		Day = data.Day;
		Evening = data.Evening;
		Night = data.Night;
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

