// Copyright (c) 2026 Neil Colvin.
// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)




namespace SimpleWeather;

/// <summary>
/// Represents the primary temperature- and pressure-related section of the weather payload.
/// </summary>
public class Main
	{
	/// <summary>Creates this model from its OpenWeather JSON fragment.</summary>
	/// <param name="json">The JSON fragment, or null for an empty model.</param>
	/// <returns>The parsed weather model.</returns>
	public static Main FromJson (string? json) => new (ResponseJson.ReadOptional<MainResponse> (json));

	internal Main (MainResponse? data)
		{
		if (data == null) return;
		Temperature = data.Temperature;
		FeelsLike = data.FeelsLike;
		TemperatureMin = data.TemperatureMin;
		TemperatureMax = data.TemperatureMax;
		Pressure = data.Pressure;
		Humidity = data.Humidity;
		SeaLevel = data.SeaLevel;
		GroundLevel = data.GroundLevel;
		DewPoint = data.DewPoint;
		}

	/// <summary>
	/// Temperature. Default Unit: Celsius
	/// </summary>
	public double? Temperature
		{
		get;
		}
	/// <summary>
	/// Temperature. This temperature parameter accounts for the human perception of weather. Default Unit: Celsius
	/// </summary>
	public double? FeelsLike
		{
		get;
		}
	/// <summary>
	/// Minimum temperature at the moment. This is minimal currently observed temperature (within large megalopolises and urban areas). Default Unit: Celsius
	/// </summary>
	public double? TemperatureMin
		{
		get;
		}
	/// <summary>
	/// Maximum temperature at the moment. This is maximal currently observed temperature (within large megalopolises and urban areas). Default Unit: Celsius
	/// </summary>
	public double? TemperatureMax
		{
		get;
		}
	/// <summary>
	/// Atmospheric pressure (on the sea level, if there is no SeaLevel or GroundLevel data), hPa
	/// </summary>
	public double? Pressure
		{
		get;
		}
	/// <summary>
	/// Humidity, %
	/// </summary>
	public double? Humidity
		{
		get;
		}
	/// <summary>
	/// Atmospheric pressure on the sea level, hPa
	/// </summary>
	public double? SeaLevel
		{
		get;
		}
	/// <summary>
	/// Atmospheric pressure on the ground level, hPa
	/// </summary>
	public double? GroundLevel
		{
		get;
		}
	/// <summary>
	/// Dew point temperature in Celsius.
	/// </summary>
	public double? DewPoint
		{
		get;
		}
	}

