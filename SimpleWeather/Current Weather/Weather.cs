// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System.Globalization;
using System.Linq;

using Newtonsoft.Json.Linq;

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
	/// <summary>
	/// Initializes a new instance of the <see cref="Weather"/> class from a JSON token.
	/// </summary>
	/// <param name="data">The JSON array that contains the weather description.</param>
	public Weather (JToken? data)
		{
		if (data != null)
			{
			ID = int.Parse (data.FirstOrDefault ()?.SelectToken ("id")?.ToString () ?? "0", CultureInfo.InvariantCulture);
			Main = data.FirstOrDefault ()?.SelectToken ("main")?.ToString () ?? string.Empty;
			Description = data.FirstOrDefault ()?.SelectToken ("description")?.ToString () ?? string.Empty;
			Icon = data.FirstOrDefault ()?.SelectToken ("icon")?.ToString () ?? string.Empty;
			}
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

