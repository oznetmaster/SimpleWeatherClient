// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)




namespace SimpleWeather;

/// <summary>
/// Represents the coordinate payload returned from the OpenWeather current weather response.
/// </summary>
public class Coordinates
	{
	/// <summary>Creates this model from its OpenWeather JSON fragment.</summary>
	/// <param name="json">The JSON fragment, or null for an empty model.</param>
	/// <returns>The parsed weather model.</returns>
	public static Coordinates FromJson (string? json) => new (ResponseJson.ReadOptional<CoordinatesResponse> (json));

	internal Coordinates (CoordinatesResponse? data)
		{
		if (data == null) return;
		Latitude = data.Latitude;
		Longitude = data.Longitude;
		}

	/// <summary>
	/// City geo location, longitude
	/// </summary>
	public double? Longitude
		{
		get;
		}
	/// <summary>
	/// City geo location, latitude
	/// </summary>
	public double? Latitude
		{
		get;
		}
	}

// No changes needed here for LatLong, as this class only exposes Latitude/Longitude as nullable properties.
