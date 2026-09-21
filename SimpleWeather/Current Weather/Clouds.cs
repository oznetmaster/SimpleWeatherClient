// Copyright (c) 2026 Neil Colvin.
// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System.Globalization;


namespace SimpleWeather;

/// <summary>
/// Represents the cloud coverage portion of a current weather response.
/// </summary>
public class Clouds
	{
	/// <summary>Creates this model from its OpenWeather JSON fragment.</summary>
	/// <param name="json">The JSON fragment, or null for an empty model.</param>
	/// <returns>The parsed weather model.</returns>
	public static Clouds FromJson (string? json) => new (ResponseJson.ReadCloudiness (json));

	internal Clouds (double? data)
		{
		Cloudiness = data;
		}

	/// <summary>
	/// Cloudiness, %
	/// </summary>
	public double? Cloudiness
		{
		get;
		}
	}

