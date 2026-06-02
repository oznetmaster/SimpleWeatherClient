// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using Newtonsoft.Json.Linq;

namespace SimpleWeather;

/// <summary>
/// Represents a coordinate pair expressed in decimal degrees.
/// </summary>
public readonly struct LatLong (double latitude, double longitude)
	{
	/// <summary>
	/// Gets the latitude component in decimal degrees.
	/// </summary>
	public double Latitude { get; init; } = latitude;

	/// <summary>
	/// Gets the longitude component in decimal degrees.
	/// </summary>
	public double Longitude { get; init; } = longitude;

	/// <inheritdoc/>
	public override string ToString () => $"{Latitude}, {Longitude}";
	}

/// <summary>
/// Provides helper extension-style methods for optional JSON extraction.
/// </summary>
internal static class Utility
	{
	/// <summary>
	/// Attempts to read a JSON token as a <see cref="double"/> returning <c>null</c> when parsing fails.
	/// </summary>
	/// <param name="data">The JSON token that contains the data.</param>
	/// <param name="element">The JSON path of the desired attribute.</param>
	/// <returns>The parsed double value or <c>null</c> if it cannot be parsed.</returns>
	internal static double? OptDouble (JToken? data, string element) => 
		data == null ? null : double.TryParse (data.SelectToken (element)?.ToString (), out var result) ? result : null;

	/// <summary>
	/// Attempts to read a JSON token as an <see cref="int"/> returning <c>null</c> when parsing fails.
	/// </summary>
	/// <param name="data">The JSON token that contains the data.</param>
	/// <param name="element">The JSON path of the desired attribute.</param>
	/// <returns>The parsed integer value or <c>null</c> if it cannot be parsed.</returns>
	internal static int? OptInt (JToken? data, string element) =>
		data == null ? null : int.TryParse (data.SelectToken (element)?.ToString (), out var result) ? result : null;
	}

