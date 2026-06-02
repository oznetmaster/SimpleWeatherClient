// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using Newtonsoft.Json.Linq;

using static SimpleWeather.Utility;

namespace SimpleWeather;

/// <summary>
/// Represents the coordinate payload returned from the OpenWeather current weather response.
/// </summary>
public class Coordinates
	{
	/// <summary>
	/// Initializes a new instance of the <see cref="Coordinates"/> class from raw JSON data.
	/// </summary>
	/// <param name="data">The JSON token containing the coordinate fields.</param>
	public Coordinates (JToken data)
		{
		if (data != null)
			{
			Longitude = OptDouble (data, "lon");
			Latitude = OptDouble (data, "lat");
			}
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
