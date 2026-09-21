// Copyright (c) 2026 Neil Colvin.
// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System.Globalization;
using System.Linq;


namespace SimpleWeather;

/// <summary>
/// Describes the pixel density variants supported by OpenWeather icon assets.
/// </summary>
public enum WeatherIconResolution
{
    /// <summary>
    /// Standard 1x icon (approximately 50x50 px).
    /// </summary>
    Standard = 1,
    /// <summary>
    /// High-DPI 2x icon for Retina/HiDPI displays.
    /// </summary>
    DoubleScale = 2,
    /// <summary>
    /// Ultra-high-definition 4x icon.
    /// </summary>
    Quadruple = 4
}

/// <summary>
/// Represents a single entry from the OpenWeather <c>weather</c> collection.
/// </summary>
public class Weather
	{
	/// <summary>Creates this model from its OpenWeather JSON fragment.</summary>
	/// <param name="json">The JSON fragment, or null for an empty model.</param>
	/// <returns>The parsed weather model.</returns>
	public static Weather FromJson (string? json) => new (ResponseJson.ReadOptional<ConditionResponse[]> (json));

	internal Weather (ConditionResponse[]? data)
		{
		if (data == null) return;
		ConditionResponse? first = data.FirstOrDefault ();
		ID = first?.ID ?? 0;
		Main = first?.Main ?? string.Empty;
		Description = first?.Description ?? string.Empty;
		Icon = first?.Icon ?? string.Empty;
		}

	/// <summary>
	/// Initializes a new, empty instance of the <see cref="Weather"/> class.
	/// </summary>
	public Weather ()
		{
		}

	/// <summary>
	/// Weather condition ID
	/// </summary>
	public int ID { get; init; }
	/// <summary>
	/// Group of weather parameters (Rain, Snow, Extreme etc.)
	/// </summary>
	public string? Main { get; init; }
	/// <summary>
	/// Weather condition within the group
	/// </summary>
	public string? Description { get; init; }
	/// <summary>
	/// Weather icon ID
	/// </summary>
	public string? Icon { get; init; }

	/// <summary>
	/// Builds the CDN URL for this weather icon using the documented OpenWeather pattern.
	/// </summary>
	/// <param name="resolution">Desired image scale (1x, 2x, or 4x).</param>
	/// <returns>The full icon URL or <c>null</c> when the icon code is unavailable.</returns>
	public string? GetIconUrl (WeatherIconResolution resolution = WeatherIconResolution.DoubleScale)
		{
		if (string.IsNullOrWhiteSpace (Icon))
			{
			return null;
			}

		var suffix = resolution switch
			{
			WeatherIconResolution.Quadruple => "@4x",
			WeatherIconResolution.DoubleScale => "@2x",
			_ => string.Empty
			};

		return $"https://openweathermap.org/img/wn/{Icon}{suffix}.png";
		}
	}

