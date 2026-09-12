// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using Newtonsoft.Json.Linq;

using static SimpleWeather.Utility;

namespace SimpleWeather;

/// <summary>
/// An object that provides current weather data
/// </summary>
public class CurrentWeather
	{
	/// <summary>
	/// Initializes a new instance of the <see cref="CurrentWeather"/> class from a JSON response.
	/// </summary>
	/// <param name="jsonResponse">The raw JSON returned by the weather API.</param>
	public CurrentWeather (string jsonResponse)
		{
		var data = JObject.Parse (jsonResponse);

		// Current Weather API can return cod as string.
		var statusCode = OptInt (data, "cod");
		if (!statusCode.HasValue)
			{
			if (int.TryParse (data.SelectToken ("cod")?.ToString (), out var scParsed))
				{
				statusCode = scParsed;
				}
			}

		if (statusCode.HasValue && statusCode.Value != 200)
			{
			StatusCode = statusCode;
			return;
			}

		// One Call uses a `current` object; Current Weather uses top-level fields.
		JToken root = data.SelectToken ("current") ?? data;

		Coordinates = GeoUtils.GetCoordinatesFromJToken (data.SelectToken ("coord") ?? data);
		Main = new Main (root.SelectToken ("main") ?? root);
		Visibility = OptDouble (root, "visibility") ?? OptDouble (data, "visibility");
		Wind = new Wind (root.SelectToken ("wind") ?? new JObject
			{
			["speed"] = root["wind_speed"],
			["deg"] = root["wind_deg"],
			["gust"] = root["wind_gust"]
			});
		Clouds = new Clouds (root.SelectToken ("clouds"));
		Rain = new Rain (root.SelectToken ("rain"));
		Snow = new Snow (root.SelectToken ("snow"));
		Sys = new Sys (root.SelectToken ("sys") ?? data.SelectToken ("sys") ?? root);
		Weather = new Weather (root.SelectToken ("weather"));

		TimezoneOffset = (OptInt (data, "timezone_offset") ?? OptInt (data, "timezone") ?? 0) / 3600;
		Timezone = data.SelectToken ("timezone")?.ToString ();
		Base = data.SelectToken ("base")?.ToString ();
		CityID = OptInt (data, "id");
		City = data.SelectToken ("name")?.ToString ();
		}

	/// <summary>
	/// Gets the coordinates associated with the weather reading.
	/// </summary>
	public LatLong Coordinates { get; }
	/// <summary>
	/// Gets the collection of high-level weather descriptors.
	/// </summary>
	public Weather? Weather { get; }
	/// <summary>
	/// Gets the OpenWeather base station identifier.
	/// </summary>
	public string? Base { get; }
	/// <summary>
	/// Gets the temperature and pressure readings.
	/// </summary>
	public Main? Main { get; }
	/// <summary>
	/// Gets the visibility distance in meters when available.
	/// </summary>
	public double? Visibility { get; }
	/// <summary>
	/// Gets the wind data for the reading.
	/// </summary>
	public Wind? Wind { get; }
	/// <summary>
	/// Gets the cloud coverage information.
	/// </summary>
	public Clouds? Clouds { get; }
	/// <summary>
	/// Gets the rainfall totals.
	/// </summary>
	public Rain? Rain { get; }
	/// <summary>
	/// Gets the snowfall totals.
	/// </summary>
	public Snow? Snow { get; }
	/// <summary>
	/// Gets the system metadata values.
	/// </summary>
	public Sys? Sys { get; }
	/// <summary>
	/// UTC (+/-) hour/s
	/// </summary>
	public int? TimezoneOffset { get; }
	/// <summary>
	/// Gets the timezone name returned by the API.
	/// </summary>
	public string? Timezone { get; }
	/// <summary>
	/// Gets the city identifier assigned by OpenWeather.
	/// </summary>
	public int? CityID { get; }
	/// <summary>
	/// Gets the city name associated with the reading.
	/// </summary>
	public string? City { get; }
	/// <summary>
	/// Gets the HTTP-like status code when an error occurs.
	/// </summary>
	public int? StatusCode { get; }
	}

