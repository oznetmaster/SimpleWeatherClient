// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using SimpleWeather;

using System;
using System.Configuration;
using System.IO;

// Read API key from App.config first, then fall back to the solution-local or AppData key files.
var apiKey = ResolveApiKey ();
if (string.IsNullOrWhiteSpace (apiKey))
	{
	Console.WriteLine ("API key is missing. Set OpenWeatherMapApiKey in App.config or create .local\\openweather-api-key.txt in the solution root.");
	return;
	}

var weatherController = new WeatherController (apiKey);
CurrentWeather? currentWeather = await weatherController.GetCurrentWeatherAsync (new LatLong (-32.07019, 115.95726), units: "metric");
WeatherForecast weatherForecast = await weatherController.GetWeatherForecastAsync (new LatLong (-32.07019, 115.95726));
var geoLocator = new GeoLocator (apiKey);
var city = await geoLocator.GetCityNameByCoordinatesAsync (new LatLong (-32.07019, 115.95726));

Console.WriteLine ($"The current weather in {city} ({currentWeather?.Coordinates.Latitude:F6}, {currentWeather?.Coordinates.Longitude:F6}) is {Math.Round (currentWeather?.Main?.Temperature ?? 0)}°C degrees with {currentWeather?.Weather?.Description}.\n");

Console.WriteLine ("The weather forecast for the next 7 days is:\n");

foreach (Daily day in weatherForecast.Daily)
	{
	// Fix: Use the null-coalescing operator to ensure DT is not null and convert it to a string.
	Console.WriteLine ($"The weather for: {day.DT?.ToString ("dd.MM.yyyy") ?? "Unknown Date"}");
	Console.WriteLine ($"Min temperature: {Math.Round (day.Temperature?.Min ?? 0)}°C");
	Console.WriteLine ($"Max temperature: {Math.Round (day.Temperature?.Max ?? 100)}°C");
	Console.WriteLine ($"The humidity will be: {day.Humidity}%");
	Console.WriteLine ($"Wind speed will be: {day.WindSpeed} m/s");
	Console.WriteLine ($"Wind direction will be: {day.WindDirectionLong} ({day.WindDirectionShort})");
	Console.WriteLine ($"The weather conditions will be: {day.Weather?.Description}");
	Console.WriteLine ($"Probability for precipitation: {day.PrecipitationProbability}%");
	Console.WriteLine ($"The moon phase will be: {day.MoonPhase}\n");
	}

CurrentWeather cw = await weatherController.GetCurrentWeatherByPostCodeAsync ("6108", "AU", "metric");
Console.WriteLine ($"The current weather in {city} ({cw.Coordinates.Latitude:F6}, {cw.Coordinates.Longitude:F6}) is {Math.Round (cw.Main?.Temperature ?? 0)}°C degrees with {cw.Weather?.Description}.\n");

static string? ResolveApiKey ()
	{
	var apiKey = ConfigurationManager.AppSettings["OpenWeatherMapApiKey"];
	if (string.IsNullOrWhiteSpace (apiKey) || IsPlaceholderApiKey (apiKey))
		{
		apiKey = TryReadLocalApiKeyFile ();
		}

	return string.IsNullOrWhiteSpace (apiKey) ? null : apiKey;
	}

static bool IsPlaceholderApiKey (string apiKey) =>
	string.Equals (apiKey, "YOUR_OPENWEATHER_API_KEY_HERE", StringComparison.Ordinal) ||
	string.Equals (apiKey, "YOUR_API_KEY_HERE", StringComparison.Ordinal);

static string? TryReadLocalApiKeyFile ()
	{
	foreach (var localConfigPath in EnumerateLocalApiKeyFilePaths ())
		{
		try
			{
			if (!File.Exists (localConfigPath))
				{
				continue;
				}

			var apiKey = File.ReadAllText (localConfigPath).Trim ();
			if (!string.IsNullOrWhiteSpace (apiKey) && !IsPlaceholderApiKey (apiKey))
				{
				return apiKey;
				}
			}
		catch
			{
			}
		}

	return null;
	}

static System.Collections.Generic.IEnumerable<string> EnumerateLocalApiKeyFilePaths ()
	{
	yield return Path.GetFullPath (Path.Combine (AppContext.BaseDirectory, "..", "..", "..", ".local", "openweather-api-key.txt"));
	yield return Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "SimpleWeather", "desktop-api-key.txt");
	}

