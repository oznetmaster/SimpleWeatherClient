// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System.Globalization;


namespace SimpleWeather;

/// <summary>
/// Represents a coordinate pair expressed in decimal degrees.
/// </summary>
/// <param name="latitude">The latitude in decimal degrees.</param>
/// <param name="longitude">The longitude in decimal degrees.</param>
public readonly struct LatLong (double latitude, double longitude)
	{
	/// <summary>
	/// Gets the latitude component in decimal degrees.
	/// </summary>
	/// <value>The latitude component in decimal degrees.</value>
	public double Latitude { get; init; } = latitude;

	/// <summary>
	/// Gets the longitude component in decimal degrees.
	/// </summary>
	/// <value>The longitude component in decimal degrees.</value>
	public double Longitude { get; init; } = longitude;

	/// <inheritdoc/>
	public override string ToString () => $"{Latitude}, {Longitude}";
	}

