// Copyright (c) 2026 Neil Colvin.
// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)




namespace SimpleWeather;

/// <summary>
/// Represents precipitation data for the current weather response.
/// </summary>
public class Rain
	{
	/// <summary>Creates this model from its OpenWeather JSON fragment.</summary>
	/// <param name="json">The JSON fragment, or null for an empty model.</param>
	/// <returns>The parsed weather model.</returns>
	public static Rain FromJson (string? json) => new (ResponseJson.ReadOptional<PrecipitationResponse> (json));

	internal Rain (PrecipitationResponse? data)
		{
		if (data == null) return;
		OneHour = data.OneHour;
		ThreeHours = data.ThreeHours;
		}
	/// <summary>
	/// Rain volume for the last 1 hour, mm
	/// </summary>
	public double? OneHour
		{
		get;
		}
	/// <summary>
	/// Rain volume for the last 3 hours, mm
	/// </summary>
	public double? ThreeHours
		{
		get;
		}
	}

