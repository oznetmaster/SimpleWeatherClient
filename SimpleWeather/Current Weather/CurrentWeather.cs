// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)





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
	public CurrentWeather (string jsonResponse) : this (ResponseJson.Read<WeatherResponse> (jsonResponse)) { }

	internal CurrentWeather (WeatherResponse data)
		{
		if (int.TryParse (data.StatusCode, out int status) && status != 200)
			{
			StatusCode = status;
			return;
			}
		InstantResponse root = data.Current ?? data;
		Coordinates = new LatLong (data.Coordinates?.Latitude ?? data.Latitude ?? 0, data.Coordinates?.Longitude ?? data.Longitude ?? 0);
		Main = new Main (root.Main ?? root);
		Visibility = root.Visibility ?? data.Visibility;
		Wind = new Wind (root.Wind ?? new WindResponse { Speed = root.WindSpeed, Degree = root.WindDegree, Gust = root.WindGust });
		Clouds = new Clouds (root.Clouds);
		Rain = new Rain (root.Rain);
		Snow = new Snow (root.Snow);
		Sys = new Sys (root.Sys ?? data.Sys ?? new SysResponse { Sunrise = root.Sunrise, Sunset = root.Sunset });
		Weather = new Weather (root.Weather);
		int offset = int.TryParse (data.Timezone, out int parsedOffset) ? parsedOffset : 0;
		TimezoneOffset = (int)(data.TimezoneOffset ?? offset) / 3600;
		Timezone = data.Timezone;
		Base = data.Base;
		CityID = data.CityID;
		City = data.CityName;
		}

	/// <summary>
	/// Gets the coordinates associated with the weather reading.
	/// </summary>
	/// <value>The coordinates associated with the weather reading.</value>
	public LatLong Coordinates { get; }
	/// <summary>
	/// Gets the collection of high-level weather descriptors.
	/// </summary>
	/// <value>The collection of high-level weather descriptors.</value>
	public Weather? Weather { get; }
	/// <summary>
	/// Gets the OpenWeather base station identifier.
	/// </summary>
	/// <value>The OpenWeather base station identifier.</value>
	public string? Base { get; }
	/// <summary>
	/// Gets the temperature and pressure readings.
	/// </summary>
	/// <value>The temperature and pressure readings.</value>
	public Main? Main { get; }
	/// <summary>
	/// Gets the visibility distance in meters when available.
	/// </summary>
	/// <value>The visibility distance in meters when available.</value>
	public double? Visibility { get; }
	/// <summary>
	/// Gets the wind data for the reading.
	/// </summary>
	/// <value>The wind data for the reading.</value>
	public Wind? Wind { get; }
	/// <summary>
	/// Gets the cloud coverage information.
	/// </summary>
	/// <value>The cloud coverage information.</value>
	public Clouds? Clouds { get; }
	/// <summary>
	/// Gets the rainfall totals.
	/// </summary>
	/// <value>The rainfall totals.</value>
	public Rain? Rain { get; }
	/// <summary>
	/// Gets the snowfall totals.
	/// </summary>
	/// <value>The snowfall totals.</value>
	public Snow? Snow { get; }
	/// <summary>
	/// Gets the system metadata values.
	/// </summary>
	/// <value>The system metadata values.</value>
	public Sys? Sys { get; }
	/// <summary>
	/// UTC (+/-) hour/s
	/// </summary>
	/// <value>UTC (+/-) hour/s</value>
	public int? TimezoneOffset { get; }
	/// <summary>
	/// Gets the timezone name returned by the API.
	/// </summary>
	/// <value>The timezone name returned by the API.</value>
	public string? Timezone { get; }
	/// <summary>
	/// Gets the city identifier assigned by OpenWeather.
	/// </summary>
	/// <value>The city identifier assigned by OpenWeather.</value>
	public int? CityID { get; }
	/// <summary>
	/// Gets the city name associated with the reading.
	/// </summary>
	/// <value>The city name associated with the reading.</value>
	public string? City { get; }
	/// <summary>
	/// Gets the HTTP-like status code when an error occurs.
	/// </summary>
	/// <value>The HTTP-like status code when an error occurs.</value>
	public int? StatusCode { get; }
	}

